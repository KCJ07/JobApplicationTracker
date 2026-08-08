using Microsoft.AspNetCore.Identity;
using JobApplicationTracker.Models;

namespace JobApplicationTracker.Data;

// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser
{
    public ICollection<Application> Applications { get; set; } = new List<Application>();
}

