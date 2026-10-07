using Fixtrack.Services;
using Fixtrack.Data;
using Fixtrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Fixtrack.Controllers;


public class RepairJobsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly NotificationService _notifications;

    private const string JobNumberPrefix = "RF-";

    public static readonly Dictionary<RepairStatus, RepairStatus[]> AllowedMoves = new()
    {
        [RepairStatus.Received] = new[] { RepairStatus.Assigned, RepairStatus.Cancelled },
        [RepairStatus.Assigned] = new[] { RepairStatus.Diagnosing, RepairStatus.Cancelled },
        [RepairStatus.Diagnosing] = new[] { RepairStatus.AwaitingApproval, RepairStatus.Cancelled },
        [RepairStatus.AwaitingApproval] = new[] { RepairStatus.Repairing, RepairStatus.Cancelled },
        [RepairStatus.Repairing] = new[] { RepairStatus.AwaitingParts, RepairStatus.ReadyForPickup, RepairStatus.Cancelled },
        [RepairStatus.AwaitingParts] = new[] { RepairStatus.Repairing, RepairStatus.Cancelled },
        [RepairStatus.ReadyForPickup] = new[] { RepairStatus.Completed },
        [RepairStatus.Completed] = Array.Empty<RepairStatus>(),
        [RepairStatus.Cancelled] = Array.Empty<RepairStatus>()
    };

    /// <summary>
    /// Which role is allowed to move a job TO this status via SetStatus.
    /// AwaitingApproval and Cancelled are left out on purpose: approval only
    /// happens through RecordApproval, and cancelling has its own action.
    /// </summary>
    private static readonly Dictionary<RepairStatus, string> ManualMoveRole = new()
    {
        [RepairStatus.Diagnosing] = "Technician",
        [RepairStatus.Repairing] = "Technician",
        [RepairStatus.AwaitingParts] = "Technician",
        [RepairStatus.ReadyForPickup] = "Technician",
        [RepairStatus.Completed] = "Receptionist",
    };

    public RepairJobsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        NotificationService notifications)
    {
        _context = context;
        _userManager = userManager;
        _notifications = notifications;
    }

    // GET: RepairJobs
    public async Task<IActionResult> Index(RepairStatus? status)
    {
        var query = _context.RepairJobs
            .Include(j => j.Customer)
            .Include(j => j.Device)
            .Include(j => j.Technician).ThenInclude(t => t!.User)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(j => j.Status == status.Value);
        }

        var jobs = await query
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();

        ViewBag.StatusCounts = await _context.RepairJobs
            .GroupBy(j => j.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);

        ViewBag.SelectedStatus = status;

        return View(jobs);
    }

    // GET: RepairJobs/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var job = await LoadJobAsync(id.Value);
        if (job == null) return NotFound();

        await PopulateTechniciansAsync(job);
        return View(job);
    }

    // GET: RepairJobs/Create
    [Authorize(Roles = "Receptionist")]
    public async Task<IActionResult> Create()
    {
        await PopulateDevicesAsync();
        return View(new RepairJob());
    }

    // POST: RepairJobs/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Receptionist")]
    public async Task<IActionResult> Create(
        [Bind("DeviceId,ReportedProblem,DeviceCondition,RequiredSpecialization,AdditionalNotes,EstimatedCost")]
        RepairJob repairJob)
    {
        var device = await _context.Devices
            .Include(d => d.Customer)
            .FirstOrDefaultAsync(d => d.Id == repairJob.DeviceId);

        if (device is null)
        {
            ModelState.AddModelError(
                nameof(RepairJob.DeviceId),
                "Choose the device being brought in.");
        }
        else
        {
            repairJob.CustomerId = device.CustomerId;
        }

        if (ModelState.IsValid)
        {
            repairJob.JobNumber = await NextJobNumberAsync();
            repairJob.ReceivedByUserId = _userManager.GetUserId(User);
            repairJob.Status = RepairStatus.Received;
            repairJob.CreatedAt = DateTime.Now;

            _context.Add(repairJob);
            await _context.SaveChangesAsync();

            TempData["Status"] =
                $"Job {repairJob.JobNumber} opened for {device!.DisplayName}.";

            return RedirectToAction(nameof(Details), new { id = repairJob.Id });
        }

        await PopulateDevicesAsync(repairJob.DeviceId);
        return View(repairJob);
    }

    /// <summary>Receptionist-only: puts a technician on the job.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Receptionist")]
    public async Task<IActionResult> Assign(int id, int? technicianId)
    {
        var job = await LoadJobAsync(id);
        if (job == null) return NotFound();

        if (job.IsClosed)
        {
            TempData["Warning"] = "That job is closed. Reopen it before reassigning.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var candidates = await _context.Technicians
            .Include(t => t.User)
            .Include(t => t.RepairJobs)
            .ToListAsync();

        Technician? chosen;

        if (technicianId.HasValue)
        {
            chosen = candidates.FirstOrDefault(t => t.Id == technicianId.Value);

            if (chosen is null)
            {
                TempData["Warning"] = "That technician no longer exists.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (!chosen.CanHandle(job.RequiredSpecialization))
            {
                TempData["Warning"] =
                    $"{chosen.User?.FullName} does not specialise in "
                  + $"{job.RequiredSpecialization}. Assigned anyway.";
            }
        }
        else
        {
            chosen = PickTechnician(candidates, job);

            if (chosen is null)
            {
                TempData["Warning"] =
                    $"Nobody available can take a {job.RequiredSpecialization} job right now. "
                  + "The job stays unassigned until someone frees up.";

                return RedirectToAction(nameof(Details), new { id });
            }
        }

        job.TechnicianId = chosen.Id;
        job.AssignedAt = DateTime.Now;

        if (job.Status == RepairStatus.Received)
        {
            job.Status = RepairStatus.Assigned;
        }

        await _context.SaveChangesAsync();
        await AddNoteAsync(job, $"Assigned to {chosen.User?.FullName}.");
        await _notifications.NotifyAsync(chosen.UserId, $"You've been assigned to {job.JobNumber}.", job.Id);

        TempData["Status"] = $"{job.JobNumber} assigned to {chosen.User?.FullName}.";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Technician-only: records what was found and quotes a price.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Technician")]
    public async Task<IActionResult> Diagnose(int id, string? diagnosis, decimal? estimatedCost)
    {
        var job = await LoadJobAsync(id);
        if (job == null) return NotFound();

        if (string.IsNullOrWhiteSpace(diagnosis))
        {
            TempData["Warning"] = "Write down what you found before sending a quote.";
            return RedirectToAction(nameof(Details), new { id });
        }

        job.Diagnosis = diagnosis.Trim();
        job.EstimatedCost = estimatedCost;
        job.DiagnosedAt = DateTime.Now;

        if (estimatedCost.HasValue && CanMoveTo(job, RepairStatus.AwaitingApproval))
        {
            job.Status = RepairStatus.AwaitingApproval;
        }
        else if (job.Status == RepairStatus.Assigned)
        {
            job.Status = RepairStatus.Diagnosing;
        }

        await _context.SaveChangesAsync();
        await AddNoteAsync(job, $"Diagnosis recorded: {job.Diagnosis}");

        await _notifications.NotifyAsync(job.ReceivedByUserId,
            estimatedCost.HasValue
                ? $"{job.JobNumber} diagnosed - quote of GH¢ {estimatedCost:N2} ready."
                : $"{job.JobNumber} diagnosed - no quote yet.",
            job.Id);

        TempData["Status"] = estimatedCost.HasValue
            ? $"Quote of GH¢ {estimatedCost:N2} ready for the customer."
            : "Diagnosis saved. Add an estimate to send it for approval.";

        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Receptionist-only: the customer's answer to the quote.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Receptionist")]
    public async Task<IActionResult> RecordApproval(int id, bool approved, string? approvalNotes)
    {
        var job = await LoadJobAsync(id);
        if (job == null) return NotFound();

        if (job.Status != RepairStatus.AwaitingApproval)
        {
            TempData["Warning"] = "That job is not waiting on a customer decision.";
            return RedirectToAction(nameof(Details), new { id });
        }

        job.CustomerApproved = approved;
        job.ApprovalNotes = approvalNotes?.Trim();
        job.ApprovedAt = DateTime.Now;

        job.Status = approved ? RepairStatus.Repairing : RepairStatus.Cancelled;

        if (!approved)
        {
            job.CompletedAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();

        await AddNoteAsync(job, approved
            ? "Customer approved the quote. Work started."
            : $"Customer declined the quote. {job.ApprovalNotes}");

        await _notifications.NotifyAsync(job.Technician?.UserId,
            approved ? $"Customer approved {job.JobNumber}. Work can start." : $"Customer declined {job.JobNumber}.",
            job.Id);

        TempData["Status"] = approved
            ? $"{job.JobNumber} approved - work can start."
            : $"{job.JobNumber} cancelled: the customer declined the quote.";

        return RedirectToAction(nameof(Details), new { id });
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, RepairStatus status, string? workPerformed)
    {
        var job = await LoadJobAsync(id);
        if (job == null) return NotFound();

        if (!CanMoveTo(job, status))
        {
            TempData["Warning"] =
                $"A job at {job.Status} cannot move straight to {status}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (ManualMoveRole.TryGetValue(status, out var requiredRole)
            && !User.IsInRole(requiredRole))
        {
            TempData["Warning"] = $"Only a {requiredRole} can move a job to {status}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var from = job.Status;
        job.Status = status;

        if (!string.IsNullOrWhiteSpace(workPerformed))
        {
            job.WorkPerformed = workPerformed.Trim();
        }

        switch (status)
        {
            case RepairStatus.ReadyForPickup:
                job.ReadyAt = DateTime.Now;
                await _context.SaveChangesAsync();
                await AddNoteAsync(job, $"Status changed from {from} to {status}.");
                await _notifications.NotifyAsync(job.ReceivedByUserId, $"{job.JobNumber} is ready for pickup.", job.Id);

                TempData["Status"] = $"{job.JobNumber} moved to {status}.";
                return RedirectToAction(nameof(Details), new { id });

            case RepairStatus.Completed:
                job.CompletedAt = DateTime.Now;
                job.FinalCost ??= job.EstimatedCost;
                break;
        }

        await _context.SaveChangesAsync();
        await AddNoteAsync(job, $"Status changed from {from} to {status}.");

        TempData["Status"] = $"{job.JobNumber} moved to {status}.";
        return RedirectToAction(nameof(Details), new { id });
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? reason)
    {
        var job = await LoadJobAsync(id);
        if (job == null) return NotFound();

        if (!CanMoveTo(job, RepairStatus.Cancelled))
        {
            TempData["Warning"] = $"A job at {job.Status} can no longer be cancelled.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Warning"] = "Give a reason - a cancelled job with no explanation helps nobody.";
            return RedirectToAction(nameof(Details), new { id });
        }

        job.Status = RepairStatus.Cancelled;
        job.CompletedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        await AddNoteAsync(job, $"Cancelled: {reason.Trim()}");

        await _notifications.NotifyAsync(job.Technician?.UserId, $"{job.JobNumber} was cancelled: {reason.Trim()}", job.Id);
        await _notifications.NotifyAsync(job.ReceivedByUserId, $"{job.JobNumber} was cancelled: {reason.Trim()}", job.Id);

        TempData["Status"] = $"{job.JobNumber} cancelled.";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>A free-text note from whoever is looking at the job.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddNote(int id, string? note)
    {
        var job = await LoadJobAsync(id);
        if (job == null) return NotFound();

        if (string.IsNullOrWhiteSpace(note))
        {
            TempData["Warning"] = "The note was empty, so nothing was saved.";
            return RedirectToAction(nameof(Details), new { id });
        }

        await AddNoteAsync(job, note.Trim());

        TempData["Status"] = "Note added.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> SpecializationForDevice(int deviceId)
    {
        var device = await _context.Devices
            .FirstOrDefaultAsync(d => d.Id == deviceId);

        if (device is null) return NotFound();

        return Json(new { specialization = (int)SpecializationFor(device.DeviceType) });
    }

    private async Task<RepairJob?> LoadJobAsync(int id)
        => await _context.RepairJobs
            .Include(j => j.Customer)
            .Include(j => j.Device)
            .Include(j => j.Technician).ThenInclude(t => t!.User)
            .Include(j => j.Notes).ThenInclude(n => n.Technician).ThenInclude(t => t!.User)
            .FirstOrDefaultAsync(j => j.Id == id);

    private static bool CanMoveTo(RepairJob job, RepairStatus target)
        => AllowedMoves.TryGetValue(job.Status, out var moves) && moves.Contains(target);

    private static Technician? PickTechnician(IEnumerable<Technician> candidates, RepairJob job)
        => candidates
            .Where(t => t.IsAssignable && t.CanHandle(job.RequiredSpecialization))
            .OrderBy(t => t.RepairJobs.Count(j => !j.IsClosed))
            .ThenBy(t => t.Specialization == Specialization.General ? 1 : 0)
            .FirstOrDefault();

    private async Task AddNoteAsync(RepairJob job, string text)
    {
        var userId = _userManager.GetUserId(User);

        var technician = userId is null
            ? null
            : await _context.Technicians.FirstOrDefaultAsync(t => t.UserId == userId);

        _context.RepairNotes.Add(new RepairNote
        {
            RepairJobId = job.Id,
            TechnicianId = technician?.Id,
            Note = text,
            CreatedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();
    }

    private async Task<string> NextJobNumberAsync()
    {
        var issued = await _context.RepairJobs
            .Where(j => j.JobNumber.StartsWith(JobNumberPrefix))
            .Select(j => j.JobNumber)
            .ToListAsync();

        var highest = issued
            .Select(number =>
                int.TryParse(number[JobNumberPrefix.Length..], out var value) ? value : 0)
            .DefaultIfEmpty(1000)
            .Max();

        return $"{JobNumberPrefix}{highest + 1}";
    }

    private static Specialization SpecializationFor(DeviceType type) => type switch
    {
        DeviceType.Phone => Specialization.Phone,
        DeviceType.Laptop => Specialization.Laptop,
        DeviceType.Desktop => Specialization.Desktop,
        DeviceType.Tablet => Specialization.Tablet,
        DeviceType.Printer => Specialization.Printer,
        DeviceType.Television => Specialization.Television,
        _ => Specialization.General
    };

    private async Task PopulateTechniciansAsync(RepairJob job)
    {
        var technicians = await _context.Technicians
            .Include(t => t.User)
            .Include(t => t.RepairJobs)
            .ToListAsync();

        var options = technicians
            .Where(t => t.CanHandle(job.RequiredSpecialization))
            .OrderByDescending(t => t.IsAssignable)
            .ThenBy(t => t.RepairJobs.Count(j => !j.IsClosed))
            .Select(t => new
            {
                t.Id,
                Label = $"{t.User?.FullName} - {t.Specialization} "
                      + $"({t.Status}, {t.RepairJobs.Count(j => !j.IsClosed)} open)"
            })
            .ToList();

        ViewBag.Technicians = new SelectList(options, "Id", "Label", job.TechnicianId);
    }

    private async Task PopulateDevicesAsync(int? selectedDeviceId = null)
    {
        var devices = await _context.Devices
            .Include(d => d.Customer)
            .Include(d => d.RepairJobs)
            .OrderBy(d => d.Customer!.LastName)
            .ThenBy(d => d.Brand)
            .ToListAsync();

        var options = devices
            .Where(d => selectedDeviceId == d.Id || !d.RepairJobs.Any(j => !j.IsClosed))
            .Select(d => new
            {
                d.Id,
                Label = $"{d.DisplayName} - {d.Customer?.FullName}"
            })
            .ToList();

        ViewBag.Devices = new SelectList(options, "Id", "Label", selectedDeviceId);
    }
}