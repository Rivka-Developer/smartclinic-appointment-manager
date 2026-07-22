// src/app/features/client/home/client-home.component.ts
// ─────────────────────────────────────────────────────────────────────────────
// דף הבית של הלקוחה (Client Home Page).
//
// מציג:
//   1. Hero banner עם ברכה אישית לפי שם הלקוחה המחוברת
//   2. שני כרטיסי ניווט מהיר: "קביעת תור חדש" ו-"התורים שלי"
//   3. קטע "איך זה עובד?" עם 3 שלבים הסבר
//
// קבצים:
//   client-home.component.html – תבנית דף הבית
//   client-home.component.css  – עיצוב עמוד הבית
//
// דף זה הוא נקודת הכניסה לכל לקוחה לאחר כניסה למערכת.
// אין כאן logic מורכב – הדף הוא בעיקר informational + ניווט.
// ─────────────────────────────────────────────────────────────────────────────

import { Component, inject , ChangeDetectionStrategy} from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-client-home',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './client-home.component.html',
  styleUrls: ['./client-home.component.css']
})
export class ClientHomeComponent {
  /**
   * Signal לקריאה בלבד של פרטי המשתמשת המחוברת.
   * משמש להצגת שם המשתמשת ב-hero: {{ user()?.fullName }}
   */
  user = inject(AuthService).user;
}
