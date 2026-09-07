using BlazorBootstrap;
using JobApplicationTracker.Data;
using JobApplicationTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace JobApplicationTracker.Services
{
    public class ApplicationService : IApplicationService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public ApplicationService(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<(List<Application> Data, int TotalCount)> GetApplicationsAsync(int pageNumber, int pageSize, string sortString, SortDirection sortDirection, string userId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var totalCount = await context.Applications.Where(x => x.ApplicationUserId.ToString() == userId).CountAsync();

            IQueryable<Application> query = context.Applications
                .Where(x => x.ApplicationUserId.ToString() == userId)
                .Include(x => x.Job);

            switch (sortString)
            {
                case "Status":
                    query = sortDirection == SortDirection.Descending
                        ? query.OrderByDescending(x => x.Status)
                        : query.OrderBy(x => x.Status);
                    break;
                case "HeardBack":
                    query = sortDirection == SortDirection.Descending
                        ? query.OrderByDescending(x => x.HeardBack)
                        : query.OrderBy(x => x.HeardBack);
                    break;
                case "ReachOutDate":
                    query = sortDirection == SortDirection.Descending
                        ? query.OrderByDescending(x => x.ReachOutDate)
                        : query.OrderBy(x => x.ReachOutDate);
                    break;
                case "DateApplied":
                    query = sortDirection == SortDirection.Descending
                        ? query.OrderByDescending(x => x.DateApplied)
                        : query.OrderBy(x => x.DateApplied);
                    break;
                case "JobTitle":
                    query = sortDirection == SortDirection.Descending
                        ? query.OrderByDescending(x => x.Job.JobTitle)
                        : query.OrderBy(x => x.Job.JobTitle);
                    break;
                case "CompanyName":
                    query = sortDirection == SortDirection.Descending
                        ? query.OrderByDescending(x => x.Job.Company)
                        : query.OrderBy(x => x.Job.Company);
                    break;
                case "State":
                    query = sortDirection == SortDirection.Descending
                        ? query.OrderByDescending(x => x.Job.State)
                        : query.OrderBy(x => x.Job.State);
                    break;
                case "AppType":
                    query = sortDirection == SortDirection.Descending
                        ? query.OrderByDescending(x => x.Job.AppType)
                        : query.OrderBy(x => x.Job.AppType);
                    break;
            }
            var applications = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (applications, totalCount);
        }

        public async Task CreateApplicationAsync(string userId, ApplicationStatus status, bool heardBack, DateOnly reachOutDate, DateOnly dateApplied, string notes, string jobTitle, string company, string website, ApplicationType appType, string state, string description, string linkedlnRecruiter)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var job = new Job
            {
                JobTitle = jobTitle,
                Company = company,
                Website = website,
                AppType = appType,
                State = state,
                Description = description,
                LinkedlnRecruiter = linkedlnRecruiter
            };

            var application = new Application
            {
                Status = status,
                HeardBack = heardBack,
                ReachOutDate = reachOutDate,
                DateApplied = dateApplied,
                Notes = notes,
                ApplicationUserId = userId,
                Job = job
            };

            context.Add(application);
            await context.SaveChangesAsync();
        }

        public async Task DeleteApplicationAsync(int appId, string userId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            await context.Applications
                .Where(x => x.ApplicationUserId == userId)
                .Where(x => x.Id == appId)
                .ExecuteDeleteAsync();
        }

        public async Task UpdateApplicationAsync(int appId, string userId, ApplicationStatus status, bool heardBack, DateOnly reachOutDate, DateOnly dateApplied, string notes, string jobTitle, string company, string website, ApplicationType appType, string state, string description, string linkedlnRecruiter)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var query = await context.Applications
                .Include(x => x.Job)
                .Where(x => x.ApplicationUserId == userId)
                .Where(x => x.Id == appId)
                .FirstOrDefaultAsync();

            if (query == null)
            {
                return;
            }

            query.Status = status;
            query.HeardBack = heardBack;
            query.ReachOutDate = reachOutDate;
            query.DateApplied = dateApplied;
            query.Notes = notes;
            query.Job.JobTitle = jobTitle;
            query.Job.Company = company;
            query.Job.Website = website;
            query.Job.AppType = appType;
            query.Job.State = state;
            query.Job.Description = description;
            query.Job.LinkedlnRecruiter = linkedlnRecruiter;

            await context.SaveChangesAsync();
        }
    }
}
