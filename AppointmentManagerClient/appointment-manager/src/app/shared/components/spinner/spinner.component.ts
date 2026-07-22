// src/app/shared/components/spinner/spinner.component.ts
// ─────────────────────────────────────────────────────────────────────────────
// רכיב ספינר (Spinner Component).
//
// רכיב שמשמש כאינדיקטור טעינה בכל רחבי האפליקציה.
// מקבל:
//   show  – האם להציג את הספינר? (false = הרכיב מוסתר ע"י *ngIf)
//   text  – טקסט אופציונלי מתחת לספינר (למשל "טוען תורים...")
//
// שימוש: <app-spinner [show]="loading()" text="טוען נתונים..." />
// ─────────────────────────────────────────────────────────────────────────────

import { Component, Input , ChangeDetectionStrategy} from '@angular/core';
@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-spinner',
  standalone: true,
  imports: [],
  template: `
    @if (show) {
      <div class="spinner-wrap">
        <div class="spinner-ring"></div>
        @if (text) {
          <p class="spinner-text">{{ text }}</p>
        }
      </div>
    }
  `,
  styles: [`
    /* מיכל: ממרכז את הספינר והטקסט אנכית ואופקית */
    .spinner-wrap {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 14px;
      padding: 48px;
    }

    /* עיגול הספינר: גבול עליון כחול (primary) על רקע אפור */
    .spinner-ring {
      width: 40px;
      height: 40px;
      border: 3px solid var(--border);       /* גבול אפור בשאר הצדדים */
      border-top-color: var(--primary);       /* גבול כחול למעלה – יוצר אפקט סיבוב */
      border-radius: 50%;
      animation: spin .7s linear infinite;   /* אנימציה גלובלית מוגדרת ב-styles.scss */
    }

    .spinner-text {
      color: var(--text-muted);
      font-size: 0.875rem;
    }
  `]
})
export class SpinnerComponent {
  /** האם להציג את הספינר? ברירת מחדל: false (מוסתר) */
  @Input() show = false;

  /** טקסט להצגה מתחת לספינר; ריק = לא מוצג */
  @Input() text = '';
}
