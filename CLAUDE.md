# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

כל מה שאתה יכול לכתוב בעברית תכתוב בעברית
## Project Overview

SmartClinic is a full-stack appointment management system with an ASP.NET Core 8 backend (Clean Architecture) and Angular 21 frontend. The app supports two roles — Client and Admin — with Hebrew RTL UI, JWT auth, and Hangfire background jobs for email reminders.

## Commands

### Backend (C# / ASP.NET Core)

```powershell
# Run API (Swagger at /swagger, Hangfire dashboard at /hangfire)
cd AppointmentManager
dotnet run --project AppointmentManager.Api

# Build solution
dotnet build

# Run tests
dotnet test

# Run single test project
dotnet test AppointmentManager.Tests

# EF Core migrations (run from solution root)
dotnet ef migrations add <MigrationName> --project AppointmentManager.Infrastructure --startup-project AppointmentManager.Api
dotnet ef database update --project AppointmentManager.Infrastructure --startup-project AppointmentManager.Api
```

### Frontend (Angular)

```powershell
cd AppointmentManagerClient\appointment-manager

npm install
ng serve           # Dev server at http://localhost:4200
ng build           # Dev build
ng build --configuration production
npx tsc --noEmit   # TypeScript type check without emitting
```

## Architecture

### Backend — Clean Architecture (4 layers)

- **Domain** (`AppointmentManager.Domain`): Core entities (`User`, `Appointment`, `WorkShift`, `SystemSettings`) and repository/service interfaces. No dependencies on other layers.
- **Application** (`AppointmentManager.Application`): Business logic services, DTOs, AutoMapper profiles. Depends only on Domain.
- **Infrastructure** (`AppointmentManager.Infrastructure`): EF Core `DbContext`, repository implementations, migrations, email/Hangfire services.
- **API** (`AppointmentManager.Api`): Controllers, middleware, DI wiring in `Program.cs`. Auto-runs EF migrations on startup and seeds default `SystemSettings`.

**Key backend services:**
- `AvailabilityService` — checks slot availability against `WorkShift` schedules and `SystemSettings` rules
- `BackgroundJobService` — Hangfire jobs: hourly appointment reminders, daily admin report at 20:00
- `AuthService` — JWT generation, BCrypt password validation
- Unit of Work pattern wraps repository access

### Frontend — Angular 21 Standalone

State is managed with **Angular Signals** (no NgRx). All components are standalone with lazy-loaded feature routes.

```
src/app/
├── core/
│   ├── services/      # AuthService, AppointmentsService, WorkShiftsService, etc.
│   ├── guards/        # Auth + role guards
│   ├── interceptors/  # JwtInterceptor (adds Bearer token to every request)
│   └── models/        # TypeScript interfaces
├── features/
│   ├── auth/          # Login, Register
│   ├── client/        # Client dashboard, new appointment, appointment list
│   └── admin/         # Admin dashboard, calendar, user management, settings
└── shared/            # Header, Snackbar, Spinner, EmptyState components
```

`app.config.ts` registers all providers. `app.routes.ts` defines lazy-loaded routes with guards.

### Data Flow

```
Angular Component → Service (Signals) → HttpClient + JwtInterceptor
  → API Controller → Application Service → Repository/UnitOfWork → EF Core → SQL Server
```

### Key Ports

| Service | URL |
|---|---|
| Frontend | http://localhost:4200 |
| Backend API | https://localhost:7001/api |
| Swagger | https://localhost:7001/swagger |
| Hangfire | https://localhost:7001/hangfire |

### Configuration

- Dev JWT key and connection string are in `AppointmentManager.Api/appsettings.Development.json`
- Database: SQL Server LocalDB (`SmartClinicDB`)
- CORS: dev allows `http://localhost:4200`; production uses `AllowedOrigins` array in `appsettings.json`
- Email (SMTP) and rate limiting configured in `appsettings.json`
- `SystemSettings` entity holds business rules (appointment buffer, session durations, cancellation deadline); seeded on first run

### Hebrew / RTL

The frontend is fully RTL with Hebrew (`Heebo` + `Rubik` fonts). A `HebrewCalendarService` handles Hebrew date display in the admin calendar. Backend comments are also written in Hebrew.
