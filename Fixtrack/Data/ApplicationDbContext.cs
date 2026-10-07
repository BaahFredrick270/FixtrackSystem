using Fixtrack.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Fixtrack.Data;

/// <summary>
/// The single database session for the whole app. Inheriting
/// IdentityDbContext&lt;ApplicationUser&gt; means the login tables and the
/// FixTrack tables live in one database and one context.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Technician> Technicians => Set<Technician>();
    public DbSet<RepairJob> RepairJobs => Set<RepairJob>();
    public DbSet<RepairNote> RepairNotes => Set<RepairNote>();
    public DbSet<Part> Parts => Set<Part>();
    public DbSet<PartUsed> PartsUsed => Set<PartUsed>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Complaint> Complaints => Set<Complaint>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Must be called first - it configures all the Identity tables.
        base.OnModelCreating(builder);

        // ---------- Customer / Device ----------
        builder.Entity<Device>()
            .HasOne(d => d.Customer)
            .WithMany(c => c.Devices)
            .HasForeignKey(d => d.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Customer>().HasIndex(c => c.PhoneNumber);
        builder.Entity<Customer>().HasIndex(c => c.LastName);
        builder.Entity<Device>().HasIndex(d => d.SerialNumber);

        // ---------- Technician ----------
        // One login account = at most one technician profile.
        builder.Entity<Technician>()
            .HasOne(t => t.User)
            .WithOne()
            .HasForeignKey<Technician>(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Technician>().HasIndex(t => t.UserId).IsUnique();
        builder.Entity<Technician>().HasIndex(t => t.Status);

        // ---------- RepairJob ----------
        // Customer and Device are both Restrict: losing either would
        // destroy the meaning of the job.
        builder.Entity<RepairJob>()
            .HasOne(j => j.Customer)
            .WithMany(c => c.RepairJobs)
            .HasForeignKey(j => j.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<RepairJob>()
            .HasOne(j => j.Device)
            .WithMany(d => d.RepairJobs)
            .HasForeignKey(j => j.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        // Notifications are personal; losing the user or the job should remove
        // them rather than leave a dangling reference nobody can act on.
        builder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Notification>()
            .HasOne(n => n.RepairJob)
            .WithMany()
            .HasForeignKey(n => n.RepairJobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Notification>().HasIndex(n => new { n.UserId, n.IsRead });

        // SetNull: removing a technician just leaves the job needing reassignment,
        // which is already a valid state.
        builder.Entity<RepairJob>()
            .HasOne(j => j.Technician)
            .WithMany(t => t.RepairJobs)
            .HasForeignKey(j => j.TechnicianId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<RepairJob>().HasIndex(j => j.JobNumber).IsUnique();
        builder.Entity<RepairJob>().HasIndex(j => j.Status);
        builder.Entity<RepairJob>().HasIndex(j => j.CreatedAt);

        // ---------- children of a repair ----------
        // Cascade here is correct: these rows have no meaning without
        // their parent job.
        builder.Entity<RepairNote>()
            .HasOne(n => n.RepairJob)
            .WithMany(j => j.Notes)
            .HasForeignKey(n => n.RepairJobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RepairNote>()
            .HasOne(n => n.Technician)
            .WithMany()
            .HasForeignKey(n => n.TechnicianId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<PartUsed>()
            .HasOne(p => p.RepairJob)
            .WithMany(j => j.PartsUsed)
            .HasForeignKey(p => p.RepairJobId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict: a part that has been used somewhere cannot be deleted
        // from the catalogue, or old bills would lose their line items.
        builder.Entity<PartUsed>()
            .HasOne(p => p.Part)
            .WithMany(p => p.UsedIn)
            .HasForeignKey(p => p.PartId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Payment>()
            .HasOne(p => p.RepairJob)
            .WithMany(j => j.Payments)
            .HasForeignKey(p => p.RepairJobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Complaint>()
            .HasOne(c => c.RepairJob)
            .WithMany(j => j.Complaints)
            .HasForeignKey(c => c.RepairJobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Complaint>().HasIndex(c => c.Status);

        builder.Entity<NotificationLog>()
            .HasOne(n => n.RepairJob)
            .WithMany(j => j.ContactHistory)
            .HasForeignKey(n => n.RepairJobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Part>().HasIndex(p => p.Name);
    }
}
