
namespace JobApplicationTracker.Models;

public class Job
{
    public int Id { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public ApplicationType AppType { get; set; }
    public string State { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? LinkedlnRecruiter { get; set; } 

    public ICollection<Application> Applications { get; set; } = new List<Application>();
}

public enum ApplicationType
{
    Internship,
    Job
}