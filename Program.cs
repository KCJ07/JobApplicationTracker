using JobApplicationTracker.Components;
using JobApplicationTracker.Components.Account;
using JobApplicationTracker.Data;
using JobApplicationTracker.Models;
using JobApplicationTracker.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddBlazorBootstrap();
builder.Services.AddScoped<IApplicationService, ApplicationService>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();


// seed user data
using (var scoped = app.Services.CreateScope())
{
    var scope = scoped.ServiceProvider;

    var context = scope.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();

    var userManager = scope.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.GetRequiredService<RoleManager<IdentityRole>>();

    if (await roleManager.FindByNameAsync("Admin") is null)
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));

    }
    if (await roleManager.FindByNameAsync("Guest") is null)
    {
        await roleManager.CreateAsync(new IdentityRole("Guest"));
    }

    if (await userManager.FindByNameAsync("johanson@grinnell.edu") is null)
    {
        var newUser = new ApplicationUser();
        newUser.UserName = "johanson@grinnell.edu";
        newUser.Email = "johanson@grinnell.edu";
        newUser.EmailConfirmed = true;


        var user = await userManager.CreateAsync(newUser, "adminPass#123");
        if (user.Succeeded == false)
        {
            Console.WriteLine("Account Creation failed for " + newUser.UserName);
        }
        else
        {
            await userManager.AddToRoleAsync(newUser, "Admin");
        }

    }

    if (await userManager.FindByNameAsync("MCrodriguez37@grinnell.edu") is null)
    {
        var newUser = new ApplicationUser()
        {
            UserName = "MCrodriguez37@grinnell.edu",
            Email = "MCrodriguez37@grinnell.edu",
            EmailConfirmed = true
        };


        var user = await userManager.CreateAsync(newUser, "guestPass#123");
        if (user.Succeeded == false)
        {
            Console.WriteLine("Account Creation failed for " + newUser.UserName);
        }
        else
        {
            await userManager.AddToRoleAsync(newUser, "Guest");
        }


    }



}

// seed job application data for johanson@ user (admin)
using (var scope = app.Services.CreateScope())
{
    var scoped = scope.ServiceProvider;
    var appService = scoped.GetRequiredService<IApplicationService>();
    var userManager = scoped.GetRequiredService<UserManager<ApplicationUser>>();
    var dbContext = scoped.GetRequiredService<ApplicationDbContext>();
    var user = await userManager.FindByNameAsync("johanson@grinnell.edu");

    if (user is not null)
    {
     bool alreadySeeded = await dbContext.Applications
    .Include(a => a.Job)
    .AnyAsync(a =>
        a.ApplicationUserId == user.Id &&
        a.Job.JobTitle == "Software Engineer" &&
        a.Job.Company == "Acme Corp");

        if (!alreadySeeded)
        {
            await appService.CreateApplicationAsync(
                userId: user.Id.ToString(),
                status: ApplicationStatus.Applied,
                heardBack: false,
                reachOutDate: new DateOnly(2026, 8, 25),
                dateApplied: new DateOnly(2026, 8, 15),
                notes: "",
                jobTitle: "Software Engineer",
                company: "Acme Corp",
                website: "acme.com",
                appType: ApplicationType.FullTime,
                state: "AZ",
                description: "A test Job",
                linkedlnRecruiter: "N/A"
                    );
        }
    }
}



    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseMigrationsEndPoint();
    }
    else
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
