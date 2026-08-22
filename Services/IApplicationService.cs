using BlazorBootstrap;
using JobApplicationTracker.Data;
using JobApplicationTracker.Models;

namespace JobApplicationTracker.Services
{
    public interface IApplicationService
    {
        Task<(List<Application> Data, int TotalCount)> GetApplicationsAsync(
    int pageNumber, int pageSize, string sortString, SortDirection sortDirection, string userId);

        Task CreateApplicationAsync(string userId, ApplicationStatus status, bool heardBack, DateOnly reachOutDate, DateOnly dateApplied, string notes, string jobTitle, string company, string website, ApplicationType appType, string state, string description, string linkedlnRecruiter);

        Task DeleteApplicationAsync(int appId, string userId);
        






        }
}
