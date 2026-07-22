// src/app/features/admin/home/admin-home.component.ts
// ─────────────────────────────────────────────────────────────────────────────
// דף הבית של המנהלת (Admin Home Page).
//
// מציג:
//   1. Hero banner עם ברכה אישית לפי שם המנהלת
//   2. שני כרטיסי ניווט מהיר: "יומן שבועי" ו-"לקוחות"
//   3. סיכום יכולות המערכת (3 נקודות)
//
// קבצים:
//   admin-home.component.html – תבנית דף הבית
//   admin-home.component.css  – עיצוב עמוד הבית
//
// דף זה הוא נקודת הכניסה למנהלת לאחר כניסה למערכת.
// אין כאן logic מורכב – הדף הוא בעיקר informational + ניווט.
// ─────────────────────────────────────────────────────────────────────────────

import { Component, inject , ChangeDetectionStrategy} from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-admin-home',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './admin-home.component.html',
  styleUrls: ['./admin-home.component.css']
})
export class AdminHomeComponent {
  /** Signal לקריאה בלבד של פרטי המנהלת המחוברת */
  user = inject(AuthService).user;
}
