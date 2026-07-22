# AppointmentManager - Angular 18 Frontend

## מבנה תיקיות מלא

```
appointment-manager/
├── angular.json
├── package.json
├── tsconfig.json
├── tsconfig.app.json
└── src/
    ├── index.html
    ├── main.ts
    ├── styles.scss                          ← עיצוב גלובלי + CSS Variables
    ├── environments/
    │   ├── environment.ts                   ← dev: apiUrl localhost
    │   └── environment.prod.ts              ← production url
    └── app/
        ├── app.component.ts                 ← root component
        ├── app.config.ts                    ← providers (HTTP, Router, Animations)
        ├── app.routes.ts                    ← כל הניתובים
        ├── core/
        │   ├── models/
        │   │   └── index.ts                 ← כל ה-interfaces (TS ↔ C# DTOs)
        │   ├── services/
        │   │   ├── auth.service.ts          ← login/register/logout + signals
        │   │   ├── appointments.service.ts  ← book/cancel/history + signals
        │   │   ├── work-shifts.service.ts   ← CRUD משמרות + signals
        │   │   ├── users.service.ts         ← clients list + history (admin)
        │   │   ├── settings.service.ts      ← system settings
        │   │   └── snack.service.ts         ← global notifications signal
        │   ├── interceptors/
        │   │   └── jwt.interceptor.ts       ← מוסיף Bearer token לכל בקשה
        │   └── guards/
        │       └── auth.guard.ts            ← authGuard / adminGuard / guestGuard
        ├── features/
        │   ├── auth/
        │   │   ├── login/
        │   │   │   └── login.component.ts
        │   │   └── register/
        │   │       └── register.component.ts
        │   ├── client/
        │   │   ├── new-appointment/
        │   │   │   └── new-appointment.component.ts  ← לוח שנה שבועי + dialog קביעה
        │   │   └── my-appointments/
        │   │       └── my-appointments.component.ts  ← רשימת תורים + ביטול
        │   └── admin/
        │       ├── calendar/
        │       │   └── admin-calendar.component.ts   ← לוח מנהלת + קביעה ללקוח
        │       ├── clients/
        │       │   └── clients.component.ts          ← טבלת לקוחות + היסטוריה
        │       └── shift-dialog/
        │           └── shift-dialog.component.ts     ← הוספת/מחיקת משמרות
        └── shared/
            └── components/
                ├── header/
                │   └── header.component.ts           ← navbar responsive
                ├── snackbar/
                │   └── snackbar.component.ts         ← הודעות success/error
                ├── spinner/
                │   └── spinner.component.ts          ← loading indicator
                └── empty-state/
                    └── empty-state.component.ts      ← empty screens עם CTA
```

---

## התקנה והפעלה

### 1. צור פרויקט Angular חדש
```bash
ng new appointment-manager --standalone --routing --style=scss
cd appointment-manager
```

### 2. העתק את כל הקבצים
העתק את כל הקבצים מהמבנה למעלה לתיקיות המתאימות.  
**חשוב:** החלף את הקבצים שנוצרו אוטומטית על ידי Angular CLI.

### 3. עדכן את ה-API URL
ב-`src/environments/environment.ts`:
```ts
export const environment = {
  production: false,
  apiUrl: 'https://localhost:7001/api'  // ← שנה לפורט של ה-C# שלך
};
```

### 4. הפעל
```bash
npm install
ng serve
```

---

## ניתובים

| נתיב | תיאור | הרשאה |
|------|--------|--------|
| `/login` | דף כניסה | אורחים בלבד |
| `/register` | דף הרשמה | אורחים בלבד |
| `/client/new-appointment` | קביעת תור | כל משתמש מחובר |
| `/client/my-appointments` | התורים שלי | כל משתמש מחובר |
| `/admin/calendar` | לוח שנה מנהלת | Admin בלבד |
| `/admin/clients` | ניהול לקוחות | Admin בלבד |

---

## CORS בשרת C#

ב-`Program.cs` הוסיפי:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Angular", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

app.UseCors("Angular");
```

---

## טכנולוגיות

- **Angular 18** - Standalone Components
- **Signals** - state management (ללא NgRx)
- **ReactiveFormsModule** - טפסים
- **HttpClient + Interceptor** - JWT אוטומטי
- **Lazy loading** - כל עמוד נטען בנפרד
- **CSS Variables** - design system מלא ב-`styles.scss`
- עיצוב RTL מלא עם פונטים עבריים (Heebo + Rubik)
