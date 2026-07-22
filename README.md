# SmartClinic

מערכת ניהול תורים Full-Stack מקצה לקצה — לניהול תורים, משמרות עבודה ולקוחות בקליניקות ועסקי שירות, עם ממשק בעברית מלא (RTL), אימות מבוסס תפקידים, ותזכורות אוטומטיות במייל.

**טכנולוגיות:** ASP.NET Core 8 · Angular 21 · TypeScript · SQL Server · Entity Framework Core · Hangfire · JWT · Clean Architecture

---

## תכונות עיקריות

| | |
|---|---|
| ניהול תורים | בדיקת זמינות אוטומטית מול משמרות עבודה |
| לוח שנה | לוח שנה עברי מלא לניהול ותצוגה למנהל |
| הרשאות | אימות JWT + הרשאות לפי תפקיד (Client / Admin) |
| תזכורות | תזכורות אימייל אוטומטיות שעה לפני התור (Hangfire) |
| דוחות | דוח יומי אוטומטי למנהל בכל ערב |
| החלפת תורים | לוח החלפת תורים בין לקוחות |
| Reactive UI | Angular Signals — ללא ספריית state חיצונית |
| RTL | ממשק מלא בעברית ותמיכת RTL native |

---

## ארכיטקטורה

Backend בנוי לפי **Clean Architecture** בארבע שכבות:

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

## טכנולוגיות

### Backend
- **ASP.NET Core 8** — Clean Architecture (Domain / Application / Infrastructure / API)
- **Entity Framework Core** — גישה ל-SQL Server + Migrations
- **AutoMapper** — מיפוי בין Entities ל-DTOs
- **Hangfire** — עבודות רקע: תזכורות שעתיות, דוח יומי ב-20:00
- **BCrypt** — הצפנת סיסמאות
- **JWT Bearer Auth** — אימות מבוסס טוקן

### Frontend
- **Angular 21** — Standalone Components + Lazy Loading
- **Angular Signals** — ניהול state ריאקטיבי ללא ספריית state חיצונית
- **RxJS + HttpClient** — תקשורת עם ה-API
- **Heebo + Rubik** — פונטים עבריים, עיצוב RTL מלא

---

## מבנה הפרויקט

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

## הרצה מקומית

### דרישות מקדימות
- .NET 8 SDK
- Node.js 20+
- SQL Server LocalDB

### Backend

```powershell
cd AppointmentManager
dotnet run --project AppointmentManager.Api
```

| שירות | כתובת |
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

## תפקידי משתמש

| תפקיד | הרשאות |
|---|---|
| **Client** | קביעת תורים, צפייה בהיסטוריה, החלפת תורים |
| **Admin** | ניהול משמרות, לוח שנה, ניהול לקוחות, הגדרות מערכת, דוחות |

---

## רישיון

פרויקט פרטי — כל הזכויות שמורות.
