// src/app/app.routes.ts
// ─────────────────────────────────────────────────────────────────────────────
// הגדרת מסלולי הניתוב של האפליקציה.
//
// כל נתיב מגדיר:
//   - path:          כתובת ה-URL
//   - canActivate:   Guards שבודקים הרשאות לפני הכניסה
//   - loadComponent: טעינה עצלה (lazy loading) – הקומפוננטה נטענת רק בגישה ראשונה
//
// מבנה הנתיבים:
//   /             → redirect ל-/login
//   /login        → דף כניסה (רק לאורחים)
//   /register     → דף הרשמה (רק לאורחים)
//   /client/...   → דפי לקוח (דורש כניסה)
//   /admin/...    → דפי מנהלת (דורש כניסה + הרשאת מנהל)
//   /**           → כל נתיב לא מוכר → redirect ל-/login
// ─────────────────────────────────────────────────────────────────────────────

import { Routes } from '@angular/router';
import { authGuard, adminGuard, guestGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  // נתיב ברירת מחדל – מנתב לדף הכניסה
  { path: '', redirectTo: 'login', pathMatch: 'full' },

  // ── דפי אימות (נגישים רק למשתמשים שאינם מחוברים) ──────────────────────
  {
    path: 'login',
    canActivate: [guestGuard], // אם כבר מחובר – מנתב לדף הבית שלו
    loadComponent: () => import('./features/auth/login/login.component')
      .then(m => m.LoginComponent) // טעינה עצלה – ה-bundle נטען רק כשנכנסים לנתיב
  },
  {
    path: 'register',
    canActivate: [guestGuard], // אם כבר מחובר – מנתב לדף הבית שלו
    loadComponent: () => import('./features/auth/register/register.component')
      .then(m => m.RegisterComponent)
  },

  // ── נתיבי לקוח (דורשים כניסה) ──────────────────────────────────────────
  {
    path: 'client',
    canActivate: [authGuard], // Guard בדרגת הורה – מגן על כל הנתיבים הצאצאים
    children: [
      { path: '', redirectTo: 'home', pathMatch: 'full' }, // /client → /client/home
      {
        path: 'home',
        loadComponent: () => import('./features/client/home/client-home.component')
          .then(m => m.ClientHomeComponent) // דף הבית של הלקוח
      },
      {
        path: 'new-appointment',
        loadComponent: () => import('./features/client/new-appointment/new-appointment.component')
          .then(m => m.NewAppointmentComponent) // קביעת תור חדש
      },
      {
        path: 'my-appointments',
        loadComponent: () => import('./features/client/my-appointments/my-appointments.component')
          .then(m => m.MyAppointmentsComponent) // צפייה בתורים שלי
      },
      {
        path: 'policy',
        loadComponent: () => import('./features/client/policy/policy.component')
          .then(m => m.PolicyComponent) // כללי המערכת ומדיניות תורים
      },
      {
        path: 'swap-board',
        loadComponent: () => import('./features/client/swap-board/swap-board.component')
          .then(m => m.SwapBoardComponent) // לוח העברת תורים
      }
    ]
  },

  // ── נתיבי מנהלת (דורשים כניסה + הרשאת מנהל) ────────────────────────────
  {
    path: 'admin',
    canActivate: [authGuard, adminGuard], // שני Guards: גם חייב להיות מחובר, גם חייב להיות מנהל
    children: [
      { path: '', redirectTo: 'home', pathMatch: 'full' }, // /admin → /admin/home
      {
        path: 'home',
        loadComponent: () => import('./features/admin/home/admin-home.component')
          .then(m => m.AdminHomeComponent) // דף הבית של המנהלת
      },
      {
        path: 'calendar',
        loadComponent: () => import('./features/admin/calendar/admin-calendar.component')
          .then(m => m.AdminCalendarComponent) // לוח שנה שבועי עם כל התורים והמשמרות
      },
      {
        path: 'clients',
        loadComponent: () => import('./features/admin/clients/clients.component')
          .then(m => m.ClientsComponent) // ניהול לקוחות
      },
      {
        path: 'swap-management',
        loadComponent: () => import('./features/admin/swap-management/swap-management.component')
          .then(m => m.SwapManagementComponent) // ניהול העברות תורים
      }
    ]
  },

  // כל נתיב לא מוכר – redirect לדף הכניסה (מניעת דפי 404)
  { path: '**', redirectTo: 'login' }
];
