namespace Fixtrack.ViewModels;

/// <summary>
/// Everything the landing page shows once a member of staff is signed in.
/// A ViewModel because no single entity holds all of this.
/// </summary>
public class HomeDashboardViewModel
{
    public string StaffName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    public int CustomerCount { get; set; }
    public int DeviceCount { get; set; }

    // Job counts by where they are in the workflow.
    public int OpenJobs { get; set; }
    public int UnassignedJobs { get; set; }
    public int AwaitingApproval { get; set; }
    public int ReadyForPickup { get; set; }
}
