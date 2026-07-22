# SmartClinic

A full-stack appointment management system for clinics and service businesses — built with a modern .NET backend and Angular frontend, featuring a fully Hebrew RTL UI, role-based authentication, and automated email reminders.

**Tech stack:** ASP.NET Core 8 · Angular 21 · TypeScript · SQL Server · Entity Framework Core · Hangfire · JWT · Clean Architecture

---

## Key Features

| | |
|---|---|
| Appointment Management | Automatic availability checks against work shifts |
| Calendar | Full Hebrew calendar view for admins |
| Authorization | JWT authentication with role-based access (Client / Admin) |
| Reminders | Automated email reminders one hour before appointments (Hangfire) |
| Reports | Automated daily admin report |
| Appointment Swaps | Swap board for clients to exchange appointments |
| Reactive UI | Angular Signals — no external state library |
| RTL | Fully Hebrew, native RTL interface |

---

## Architecture

The backend follows **Clean Architecture** with four layers:

```
Angular 21 (Signals + Standalone)
        |  HTTPS + JWT
        v
API Layer (Controllers)
        |
        v
Application Layer (Services / DTOs)
        |
        v
Domain Layer (Entities / Interfaces)
        |
        v
Infrastructure Layer (EF Core / SQL Server)

API Layer --> Hangfire (Background Jobs)
```

## Technologies

### Backend
- **ASP.NET Core 8** — Clean Architecture (Domain / Application / Infrastructure / API)
- **Entity Framework Core** — SQL Server access + Migrations
- **AutoMapper** — Entity-to-DTO mapping
- **Hangfire** — Background jobs: hourly reminders, daily report at 20:00
- **BCrypt** — Password hashing
- **JWT Bearer Auth** — Token-based authentication

### Frontend
- **Angular 21** — Standalone Components + Lazy Loading
- **Angular Signals** — Reactive state management, no external state library
- **RxJS + HttpClient** — API communication
- **Heebo + Rubik** — Hebrew fonts, full RTL design

---

## Project Structure

```
AppointmentManager/
├── AppointmentManager.Domain/          # Entities + Interfaces
├── AppointmentManager.Application/     # Business logic, DTOs, AutoMapper
├── AppointmentManager.Infrastructure/   # EF Core, Repositories, Email
└── AppointmentManager.Api/              # Controllers, DI, Middleware

AppointmentManagerClient/appointment-manager/
└── src/app/
    ├── core/       # Services, Guards, Interceptors, Models
    ├── features/   # auth / client / admin
    └── shared/     # Header, Snackbar, Spinner, EmptyState
```

---

## Getting Started

### Prerequisites
- .NET 8 SDK
- Node.js 20+
- SQL Server LocalDB

### Backend

```powershell
cd AppointmentManager
dotnet run --project AppointmentManager.Api
```

| Service | URL |
|---|---|
| Swagger | https://localhost:7001/swagger |
| Hangfire Dashboard | https://localhost:7001/hangfire |

### Frontend

```powershell
cd AppointmentManagerClient/appointment-manager
npm install
ng serve
```

http://localhost:4200

---

## User Roles

| Role | Permissions |
|---|---|
| **Client** | Book appointments, view history, swap appointments |
| **Admin** | Manage shifts, calendar, clients, system settings, reports |

---

## License

Private project — All rights reserved.
