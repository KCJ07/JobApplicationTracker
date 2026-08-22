# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A Blazor Web App (Interactive Server render mode) for tracking job applications, built to practice full-stack ASP.NET development. Stack: Blazor Web App + ASP.NET Identity (auth) + EF Core + SQLite.

## Commands

```
dotnet build                          # build
dotnet run                            # run (launch profile in Properties/launchSettings.json)
dotnet watch                          # run with hot reload
dotnet ef migrations add <Name>       # add a migration (run from repo root, project has EF Core Design/Tools refs)
dotnet ef database update             # apply migrations to Data/app.db
```

There is no test project in this repo yet.

## Architecture

**Domain model**: `Job` (the position: title, company, website, state, description, recruiter) and `Application` (a user's application to a `Job`: status, dates, notes) are separate entities — `Application` has a `JobId` FK and belongs to one `ApplicationUser` via `ApplicationUserId`. A `Job` can have many `Application` rows (`Models/Job.cs`, `Models/Application.cs`). `ApplicationStatus` and `ApplicationType` are enums on these models.

**Data access is service-mediated**: Razor components never touch `ApplicationDbContext` directly — they inject `IApplicationService` (`Services/IApplicationService.cs` / `ApplicationService.cs`), which wraps EF Core queries including the paging/sorting/filtering logic consumed by Blazor Bootstrap's `Grid` component. When adding a new query or mutation, extend this service interface rather than querying the context from a page.

**Auth**: ASP.NET Identity Core (not full Identity — see `AddIdentityCore` in `Program.cs`) with cookie auth, `IdentityRole` roles (`Admin`, `Guest` seeded at startup), and `ApplicationUser : IdentityUser`. Identity UI pages live under `Components/Account/` (scaffolded Identity pages/components — Login, Register, 2FA, passkeys, manage account, etc.) and are wired up via `app.MapAdditionalIdentityEndpoints()`. Confirmed accounts are required (`RequireConfirmedAccount = true`), but email sending is a no-op (`IdentityNoOpEmailSender`), so confirmation must be handled manually in dev (e.g. via seeded `EmailConfirmed = true` users or the migrations endpoint).

**Routing/authorization**: `Components/Routes.razor` wraps the router in `AuthorizeRouteView`, redirecting unauthenticated users to login by default — pages are locked down unless explicitly public. Most feature pages (e.g. `Components/Pages/Home.razor`, the main application grid/CRUD page) carry `@attribute [Authorize]` and read the current user id from the `AuthenticationState` cascading parameter, not from `HttpContext`.

**Startup seeding**: `Program.cs` seeds two roles (`Admin`, `Guest`), two demo users, and a sample `Application`/`Job` for the admin user on every startup (guarded by existence checks, so it's idempotent). When changing seed data, keep it idempotent the same way.

**Database**: SQLite file at `Data/app.db`, connection string in `appsettings.json` (`DataSource=Data/app.db;Cache=Shared`). The `.csproj` has an explicit `CopyToOutputDirectory` entry for this file. Migrations live in `Data/Migrations/`.
