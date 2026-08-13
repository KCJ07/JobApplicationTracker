using BlazorBootstrap;
using JobApplicationTracker.Models;

namespace JobApplicationTracker.Services
{
    public interface IApplicationService
    {
        Task<(List<Application> Data, int TotalCount)> GetApplicationsAsync(
    int pageNumber, int pageSize, string sortString, SortDirection sortDirection, string userId);

    }
}
