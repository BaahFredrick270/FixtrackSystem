namespace Fixtrack.ViewModels;

/// <summary>
/// Everything a technician's dashboard shows: their own work, not the
/// shop-wide picture a receptionist sees.
/// </summary>
public class TechnicianDashboardViewModel
{
    public string StaffName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    public string Specialization { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public int AssignedCount { get; set; }
    public int NeedsDiagnosisCount { get; set; }
    public int RepairingCount { get; set; }
    public int CompletedCount { get; set; }
}