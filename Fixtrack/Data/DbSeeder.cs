using Fixtrack.Models;
using Microsoft.AspNetCore.Identity;

namespace Fixtrack.Data;

/// <summary>
/// Creates the two roles and a starter account for each, so there is
/// someone to log in as on a fresh database. Safe to run every startup -
/// it checks before creating anything.
/// </summary>
public static class DbSeeder
{
    // Central place for the role names, so a typo cannot silently
    // create a second role called "Technicain".
    public const string ReceptionistRole = "Receptionist";
    public const string TechnicianRole = "Technician";

    // Starter account emails (must match what is in the database).
    private const string ReceptionistEmail = "baahfredrick08@gmail.com";
    private const string TechnicianEmail = "baahfredrick09@gmail.com";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // ----- roles -----
        foreach (var role in new[] { ReceptionistRole, TechnicianRole })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // ----- starter accounts -----
        await EnsureUserAsync(userManager,
            email: ReceptionistEmail,
            fullName: "Front Desk",
            password: "Fix@123",
            role: ReceptionistRole);

        await EnsureUserAsync(userManager,
            email: TechnicianEmail,
            fullName: "Kofi Mensah",
            password: "Fix@123",
            role: TechnicianRole);

        // ----- technician profiles -----
        // Having the Technician role is not enough: the system also needs
        // to know what this person repairs.
        var context = services.GetRequiredService<ApplicationDbContext>();

        await EnsureTechnicianAsync(context, userManager,
            email: TechnicianEmail,
            specialization: Specialization.Laptop,
            baseLocation: "Main Workshop");
    }

    private static async Task EnsureTechnicianAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        string email, Specialization specialization,
        string baseLocation)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null) return;

        // The unique index would reject a duplicate anyway, but checking
        // first gives a clear result instead of a database error.
        if (context.Technicians.Any(t => t.UserId == user.Id)) return;

        context.Technicians.Add(new Technician
        {
            UserId = user.Id,
            Specialization = specialization,
            Status = TechnicianStatus.Available,
            BaseLocation = baseLocation
        });

        await context.SaveChangesAsync();
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email, string fullName, string password, string role)
    {
        // Already there? Leave it alone - never overwrite a real password.
        if (await userManager.FindByEmailAsync(email) is not null) return;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName,
            IsActive = true
        };

        // CreateAsync hashes the password for us. Never store it as plain text.
        var result = await userManager.CreateAsync(user, password);

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, role);
        }
        else
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Could not seed {email}: {errors}");
        }
    }
}

