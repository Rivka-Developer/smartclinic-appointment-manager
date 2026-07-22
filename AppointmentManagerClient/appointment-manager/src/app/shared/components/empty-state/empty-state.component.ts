// src/app/shared/components/empty-state/empty-state.component.ts
// ─────────────────────────────────────────────────────────────────────────────
// רכיב מצב ריק (Empty State Component).
//
// מוצג כאשר אין תוכן להציג (למשל: אין תורים, אין תוצאות חיפוש).
// מציג אייקון, כותרת, תיאור, וכפתור פעולה אופציונלי.
//
// שימוש:
//   <app-empty-state
//     icon="calendar"
//     title="אין תורים"
//     description="לא קבעת תורים עדיין"
//     actionLabel="קבע תור עכשיו"
//     (action)="navigate()"
//   />
// ─────────────────────────────────────────────────────────────────────────────

import { Component, Input, Output, EventEmitter , ChangeDetectionStrategy} from '@angular/core';
import { IconComponent } from '../icon/icon.component';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-empty-state',
  standalone: true,
  imports: [IconComponent],
  templateUrl: './empty-state.component.html',
  styleUrls: ['./empty-state.component.css']
})
export class EmptyStateComponent {
  /** שם האייקון מ-IconComponent (ברירת מחדל: לוח שנה) */
  @Input() icon = 'calendar';

  /** כותרת ראשית של מצב הריק */
  @Input() title = '';

  /** תיאור/הסבר מתחת לכותרת */
  @Input() description = '';

  /** טקסט כפתור הפעולה; ריק = הכפתור לא מוצג */
  @Input() actionLabel = '';

  /** פולט אירוע בלחיצה על כפתור הפעולה */
  @Output() action = new EventEmitter<void>();
}
