using BlazorBootstrap;
using JobApplicationTracker.Data;
using Microsoft.AspNetCore.SignalR;
using JobApplicationTracker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;


namespace JobApplicationTracker.Services
{
    public class ApplicationService : IApplicationService
    {

        private readonly ApplicationDbContext _context;

        public ApplicationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(List<Application> Data, int TotalCount)> GetApplicationsAsync( int pageNumber, int pageSize, string sortString, SortDirection sortDirection, string userId)
        {
            var totalCount = await _context.Applications.Where(x => x.ApplicationUserId.ToString() == userId).CountAsync();

            IQueryable<Application> query = _context.Applications
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



            _context.Add(application);
            await _context.SaveChangesAsync();
        }



    }
}
