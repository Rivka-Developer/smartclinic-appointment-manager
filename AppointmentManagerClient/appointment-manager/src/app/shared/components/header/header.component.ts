// src/app/shared/components/header/header.component.ts
// ─────────────────────────────────────────────────────────────────────────────
// כותרת עמוד גלובלית (App Header).
//
// מוצג בכל עמוד כאשר המשתמשת מחוברת (isLoggedIn() = true).
// מציג:
//   - לוגו + שם המערכת (לחיצה מנווטת לדף הבית המתאים)
//   - תפריט ניווט: שונה ללקוחה ולמנהלת
//   - שם המשתמשת המחוברת + כפתור התנתקות
//
// הקומפוננטה מוצבת ב-app.component.ts מעל ה-<router-outlet>.
// ─────────────────────────────────────────────────────────────────────────────

import { Component, inject , ChangeDetectionStrategy} from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-header',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.css']
})
export class HeaderComponent {
  /** גישה לשירות האימות לצורך isLoggedIn(), isAdmin(), user(), logout() */
  readonly auth = inject(AuthService);
}
