// src/main.ts
// ─────────────────────────────────────────────────────────────────────────────
// נקודת הכניסה הראשית של האפליקציה.
//
// זהו הקובץ הראשון שרץ כשהדפדפן טוען את האפליקציה.
// תפקידו: להפעיל (bootstrap) את קומפוננטת השורש AppComponent
// עם ההגדרות שמוגדרות ב-appConfig.
// ─────────────────────────────────────────────────────────────────────────────

import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';       // ספקים (providers) גלובליים
import { AppComponent } from './app/app.component'; // קומפוננטת השורש

// מפעיל את האפליקציה – מחבר את AppComponent לאלמנט <app-root> ב-index.html
bootstrapApplication(AppComponent, appConfig)
  .catch((err) => console.error(err)); // מדפיס שגיאת הפעלה ל-console אם הבוט נכשל
