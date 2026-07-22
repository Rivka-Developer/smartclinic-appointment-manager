// src/app/shared/components/snackbar/snackbar.component.ts
// ─────────────────────────────────────────────────────────────────────────────
// רכיב הודעת Snackbar.
//
// מציג הודעה קצרה בתחתית המסך לאחר פעולות כמו שמירה, ביטול, שגיאה.
// קורא מ-SnackService.message() שהוא Signal – עדכון אוטומטי בכל הודעה חדשה.
//
// סוגי הודעות (type):
//   success – הצלחה (ירוק) + אייקון ✓
//   error   – שגיאה (אדום) + אייקון ✕
//   info    – מידע (כחול) + אייקון i
//
// הסטיילינג של ה-.snack ושלושת הקלאסים (success/error/info)
// מוגדר ב-styles.scss הגלובלי כ-dialog/snackbar styles.
//
// SnackService מסיר אוטומטית את ההודעה לאחר 4 שניות (setTimeout).
// ─────────────────────────────────────────────────────────────────────────────

import { Component, inject , ChangeDetectionStrategy} from '@angular/core';
import { SnackService } from '../../../core/services/snack.service';
import { IconComponent } from '../icon/icon.component';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-snackbar',
  standalone: true,
  imports: [IconComponent],
  templateUrl: './snackbar.component.html'
})
export class SnackbarComponent {
  /** קריאה ישירה ל-Signal של השירות – Angular מרנדר מחדש בכל שינוי */
  readonly snack = inject(SnackService);
}
