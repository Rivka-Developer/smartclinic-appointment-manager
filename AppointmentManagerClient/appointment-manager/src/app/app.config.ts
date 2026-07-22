// src/app/app.config.ts
// ─────────────────────────────────────────────────────────────────────────────
// הגדרות האפליקציה הגלובליות (Application Config).
//
// קובץ זה מרכז את כל ה"ספקים" (providers) של האפליקציה:
//   - ניתוב (Router)
//   - תקשורת HTTP עם interceptors
//   - אנימציות
//   - הגדרות לוקאל (שפה/מטבע/תאריכים)
//
// מועבר ל-bootstrapApplication() ב-main.ts.
// ─────────────────────────────────────────────────────────────────────────────

import { ApplicationConfig, provideZoneChangeDetection, LOCALE_ID } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { routes } from './app.routes';
import { jwtInterceptor } from './core/interceptors/jwt.interceptor';
import { registerLocaleData } from '@angular/common';
import localeHe from '@angular/common/locales/he'; // נתוני לוקאל עברית

// רישום נתוני הלוקאל העברית – נדרש לפני שניתן להשתמש ב-LOCALE_ID: 'he'
// מאפשר לפייפים כגון DatePipe ו-CurrencyPipe לעבוד בעברית
registerLocaleData(localeHe);

export const appConfig: ApplicationConfig = {
  providers: [
    // אופטימיזציה: מאחד (coalesces) אירועי שינוי zone כדי להפחית renders מיותרים
    provideZoneChangeDetection({ eventCoalescing: true }),

    // מספק את מערכת הניתוב עם הנתיבים שהוגדרו ב-app.routes.ts
    provideRouter(routes),

    // מספק את HttpClient עם ה-JWT interceptor –
    // כל בקשת HTTP שתצא תעבור דרך jwtInterceptor (שמוסיף את הטוקן)
    provideHttpClient(withInterceptors([jwtInterceptor])),

    // מספק תמיכה באנימציות Angular (דרוש לדיאלוגים ומעברים)
    provideAnimations(),

    // קובע את שפת הלוקאל לעברית – משפיע על DatePipe, NumberPipe וכו'
    { provide: LOCALE_ID, useValue: 'he' }
  ]
};
