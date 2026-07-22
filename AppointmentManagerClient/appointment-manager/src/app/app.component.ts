// src/app/app.component.ts
// ─────────────────────────────────────────────────────────────────────────────
// קומפוננטת השורש של האפליקציה (Root Component).
//
// זוהי הקומפוננטה הראשית שמכילה את כל שאר הקומפוננטות.
// היא מוצמדת לאלמנט <app-root> ב-index.html.
//
// מבנה התבנית:
//   <app-header>    – פס הניווט העליון; ה-HeaderComponent בודק isLoggedIn() בפנים
//   <main>          – אזור התוכן הראשי שבו מוצגות הקומפוננטות לפי הנתיב
//   <router-outlet> – ה"חלון" שבו Angular מציג את הקומפוננטה המתאימה לנתיב
//   <app-snackbar>  – הודעות קופצות (מוצגות מעל הכל)
// ─────────────────────────────────────────────────────────────────────────────

import { Component , ChangeDetectionStrategy} from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HeaderComponent } from './shared/components/header/header.component';
import { SnackbarComponent } from './shared/components/snackbar/snackbar.component';
import { ChatbotComponent } from './shared/components/chatbot/chatbot.component';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-root',     // מתאים ל-<app-root> ב-index.html
  standalone: true,          // קומפוננטה עצמאית – לא שייכת ל-NgModule
  imports: [RouterOutlet, HeaderComponent, SnackbarComponent, ChatbotComponent],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {}
