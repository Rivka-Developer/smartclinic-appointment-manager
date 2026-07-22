// src/app/features/admin/shift-dialog/shift-dialog.component.ts
// ─────────────────────────────────────────────────────────────────────────────
// דיאלוג ניהול משמרות (Shift Dialog).
//
// מאפשר למנהלת:
//   1. לצפות במשמרות קיימות ביום ולמחוק אותן
//   2. להוסיף משמרות חדשות: פריסטים (בוקר/ערב) או בחירה חופשית
//   3. להוסיף משמרות לכמה ימים ברצף (multi-day mode)
//
// הקומפוננטה מקבלת את date ו-existingShifts מהקומפוננטה האב (admin-calendar)
// ופולטת אירועי close ו-changed.
// ─────────────────────────────────────────────────────────────────────────────

import { Component, Input, Output, EventEmitter, signal, inject , ChangeDetectionStrategy} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { WorkShiftsService } from '../../../core/services/work-shifts.service';
import { SnackService } from '../../../core/services/snack.service';
import { HebrewCalendarService } from '../../../core/services/hebrew-calendar.service';
import { WorkShiftResponse } from '../../../core/models';
import { IconComponent } from '../../../shared/components/icon/icon.component';

/**
 * פריסט משמרת: שם, שעת התחלה ושעת סיום בפורמט "HH:mm".
 * משמש לכפתורי הבחירה המהירה בדיאלוג.
 */
interface PresetShift { label: string; start: string; end: string; }

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-shift-dialog',
  standalone: true,
  imports: [FormsModule, IconComponent],
  templateUrl: './shift-dialog.component.html',
  styleUrls: ['./shift-dialog.component.css']
})
export class ShiftDialogComponent {
  /** התאריך שעבורו מנהלים משמרות (מועבר מ-admin-calendar) */
  @Input() date!: Date;

  /** רשימת המשמרות הקיימות לתאריך זה (מועברת מ-admin-calendar) */
  @Input() existingShifts: WorkShiftResponse[] = [];

  /** פולט אירוע כאשר המשתמשת סוגרת את הדיאלוג (ללא שינוי) */
  @Output() close = new EventEmitter<void>();

  /** פולט אירוע לאחר הוספה/מחיקה של משמרת (מגרום לרענון הלוח) */
  @Output() changed = new EventEmitter<void>();

  private shiftsService  = inject(WorkShiftsService);
  private snack          = inject(SnackService);
  private hebrewCalendar = inject(HebrewCalendarService);

  /** האם בקשת שמירה/מחיקה בתהליך? */
  saving = signal(false);

  /** האם מצב ריבוי ימים פעיל? */
  multiDayMode = signal(false);

  /**
   * קבוצת הפריסטים הנבחרים (Set לתמיכה בבחירה מרובה).
   * ערכים אפשריים: 'בוקר', 'ערב', 'other'.
   */
  selectedPresets = signal<Set<string>>(new Set());

  /** שעת התחלה לבחירה חופשית (פורמט "HH:mm") */
  customStart = '';

  /** שעת סיום לבחירה חופשית (פורמט "HH:mm") */
  customEnd = '';

  /** תאריך הסיום לריבוי ימים (פורמט "YYYY-MM-DD" של sv-SE) */
  endDateStr = '';

  /**
   * פריסטים מוגדרים מראש.
   * ניתן להרחיב בעתיד על ידי הוספת אובייקטים לרשימה.
   */
  presets: PresetShift[] = [
    { label: 'בוקר', start: '09:30', end: '15:00' },
    { label: 'ערב',  start: '20:00', end: '23:00' }
  ];

  /**
   * תווית התאריך המוצגת בכותרת הדיאלוג.
   * מחזיר: "יום שני, 21 במאי | כ״א אייר"
   */
  get dateLabel(): string {
    if (!this.date) return '';
    const gregorian = this.date.toLocaleDateString('he-IL', { weekday: 'long', day: 'numeric', month: 'long' });
    const hebrew = this.hebrewCalendar.formatDate(this.date);
    return `${gregorian} | ${hebrew}`;
  }

  /** רשימת חגים ביום זה מה-HebrewCalendarService */
  get dateHolidays(): string[] {
    return this.date ? this.hebrewCalendar.getDayHolidays(this.date) : [];
  }

  /**
   * תווית כפתור ההוספה:
   *   - "הוסף משמרת" (אחת)
   *   - "הוסף X משמרות" (ריבוי)
   * מחשב: מספר פריסטים × מספר ימים.
   */
  get addButtonLabel(): string {
    const shiftsCount = this.selectedPresets().size;
    const daysCount = this.multiDayMode() && this.endDateStr
      ? this.getDatesInRange(this.date, new Date(this.endDateStr)).length
      : 1;
    const total = shiftsCount * daysCount;
    return total > 1 ? `הוסף ${total} משמרות` : 'הוסף משמרת';
  }

  /** האם הפריסט עם label זה נמצא ב-selectedPresets? */
  isSelected(label: string): boolean {
    return this.selectedPresets().has(label);
  }

  /** האם נבחרה לפחות משמרת אחת? (משמש להצגת אפשרות ריבוי ימים) */
  hasAnySelected(): boolean {
    return this.selectedPresets().size > 0;
  }

  /**
   * מחליף בחירה של פריסט (toggle).
   * אם 'other' בוטל – מנקה את customStart/customEnd.
   * אם אין בחירות – מבטל מצב ריבוי ימים.
   */
  togglePreset(label: string): void {
    this.selectedPresets.update(s => {
      const next = new Set(s);
      if (next.has(label)) next.delete(label);
      else next.add(label);
      return next;
    });
    if (!this.isSelected('other')) {
      this.customStart = '';
      this.customEnd = '';
    }
    if (!this.hasAnySelected()) {
      this.multiDayMode.set(false);
      this.endDateStr = '';
    }
  }

  /**
   * תאריך מינימלי לשדה "עד תאריך": יום אחרי date.
   * פורמט sv-SE ("YYYY-MM-DD") תואם את ה-type="date" של HTML.
   */
  get minEndDate(): string {
    if (!this.date) return '';
    const next = new Date(this.date);
    next.setDate(next.getDate() + 1);
    return next.toLocaleDateString('sv-SE');
  }

  /** מחליף מצב ריבוי ימים ומנקה את תאריך הסיום */
  toggleMultiDay(): void {
    this.multiDayMode.update(v => !v);
    this.endDateStr = '';
  }

  /**
   * האם ניתן ללחוץ על "הוסף משמרת"?
   * תנאים:
   *   - לפחות פריסט אחד נבחר
   *   - אם נבחר 'other': customStart וcustomEnd מוזנים
   *   - אם multiDayMode: endDateStr מוזן
   */
  canAdd(): boolean {
    if (!this.hasAnySelected()) return false;
    if (this.isSelected('other') && (!this.customStart || !this.customEnd)) return false;
    if (this.multiDayMode() && !this.endDateStr) return false;
    return true;
  }

  /**
   * מחזיר רשימת תאריכים (פורמט sv-SE) מ-start עד end (כולל).
   * משמש לחישוב הבקשות לריבוי ימים.
   */
  private getDatesInRange(start: Date, end: Date): string[] {
    const dates: string[] = [];
    const current = new Date(start);
    current.setHours(0, 0, 0, 0);
    const endCopy = new Date(end);
    endCopy.setHours(0, 0, 0, 0);
    while (current <= endCopy) {
      dates.push(current.toLocaleDateString('sv-SE'));
      current.setDate(current.getDate() + 1);
    }
    return dates;
  }

  /**
   * בונה רשימת אובייקטי משמרת מהבחירות הנוכחיות.
   * מוסיף ":00" לשניות (API מצפה לפורמט "HH:mm:ss").
   */
  private getShiftsToAdd(): Array<{ startTime: string; endTime: string }> {
    const shifts: Array<{ startTime: string; endTime: string }> = [];
    for (const p of this.presets) {
      if (this.isSelected(p.label)) {
        shifts.push({ startTime: p.start + ':00', endTime: p.end + ':00' });
      }
    }
    if (this.isSelected('other') && this.customStart && this.customEnd) {
      shifts.push({ startTime: this.customStart + ':00', endTime: this.customEnd + ':00' });
    }
    return shifts;
  }

  /**
   * שולח את כל בקשות ההוספה ב-parallel עם forkJoin.
   *
   * אלגוריתם:
   *   1. בונה רשימת תאריכים (יום אחד או טווח)
   *   2. בונה רשימת משמרות לכל תאריך
   *   3. flatMap → מערך כל הבקשות
   *   4. catchError → כשל בבקשה בודדת מחזיר null (לא עוצר את הכלל)
   *   5. forkJoin → מחכה לכולן, ואז מציג הודעה מסכמת
   */
  addShifts(): void {
    this.saving.set(true);
    const dates = this.multiDayMode() && this.endDateStr
      ? this.getDatesInRange(this.date, new Date(this.endDateStr))
      : [this.date.toLocaleDateString('sv-SE')]; // יום אחד בלבד
    const shifts = this.getShiftsToAdd();

    const requests = dates.flatMap(date =>
      shifts.map(shift =>
        this.shiftsService.add({ date, ...shift }).pipe(catchError(() => of(null)))
      )
    );

    forkJoin(requests).subscribe(results => {
      const succeeded = results.filter(r => r !== null).length;
      const failed    = results.length - succeeded;

      // הודעה מסכמת: הצלחה מלאה / כשל מלא / חלקי
      if (failed === 0) {
        this.snack.success(succeeded > 1 ? `${succeeded} משמרות נוספו בהצלחה` : 'משמרת נוספה בהצלחה');
      } else if (succeeded === 0) {
        this.snack.error(failed > 1 ? `${failed} משמרות לא נוספו בגלל שגיאה` : 'משמרת לא נוספה בגלל שגיאה');
      } else {
        this.snack.info(`${succeeded} משמרות נוספו, ${failed} לא נוספו בגלל שגיאה`);
      }

      // איפוס הטופס לאחר שמירה
      this.saving.set(false);
      this.selectedPresets.set(new Set());
      this.customStart = '';
      this.customEnd = '';
      this.multiDayMode.set(false);
      this.endDateStr = '';
      this.changed.emit(); // מודיע לקומפוננטה האב לרענן את הנתונים
    });
  }

  /**
   * מוחק משמרת קיימת לפי ID.
   * לאחר מחיקה: פולט changed לרענון הלוח.
   */
  deleteShift(id: string): void {
    this.saving.set(true);
    this.shiftsService.delete(id).subscribe({
      next: () => {
        this.snack.success('משמרת הוסרה');
        this.saving.set(false);
        this.changed.emit();
      },
      error: () => { this.snack.error('שגיאה בהסרת משמרת'); this.saving.set(false); }
    });
  }

  /** חותך TimeSpan ("HH:mm:ss") ל-"HH:mm" לתצוגה */
  formatTS(ts: string): string {
    return ts.substring(0, 5);
  }
}
