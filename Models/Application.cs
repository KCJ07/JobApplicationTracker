using JobApplicationTracker.Data;

namespace JobApplicationTracker.Models;

public class Application
{
    public int Id { get; set; }
    public ApplicationStatus Status { get; set; }
    public bool HeardBack { get; set; }
    public DateOnly ReachOutDate { get; set; }
    public DateOnly DateApplied { get; set; }
    public string? Notes { get; set; }

    public ApplicationUser ApplicationUser { get; set; } = null!;
    public string ApplicationUserId { get; set; } = string.Empty;

    public Job Job { get; set; } = null!;
    public int JobId { get; set; } 

    
} 

public enum ApplicationStatus
{
    Applied,
    Interviewing,
    Rejected,
    Offered
}