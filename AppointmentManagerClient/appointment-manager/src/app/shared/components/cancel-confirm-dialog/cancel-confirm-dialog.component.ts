// src/app/shared/components/cancel-confirm-dialog/cancel-confirm-dialog.component.ts
// ─────────────────────────────────────────────────────────────────────────────
// דיאלוג אישור ביטול תור – משותף ללקוחה ולמנהלת.
//
// מציג פרטי התור (תאריך עברי, לועזי, שעה) ושואל אישור לביטול.
// ה-API call עצמו נשאר בקומפוננטה האב – הדיאלוג רק מציג UI ופולט אירועים.
//
// @Input  appointment – התור שעומד להיות מבוטל
// @Input  question    – שאלת האישור (שונה בין מנהלת ללקוחה)
// @Input  cancelling  – האם הביטול בתהליך (מושבת כפתור)
// @Output confirmed   – נפלט כאשר המשתמשת לוחצת "כן, בטל"
// @Output closed      – נפלט כאשר המשתמשת לוחצת "חזרה" או מחוץ לדיאלוג
// ─────────────────────────────────────────────────────────────────────────────

import { Component, Input, Output, EventEmitter, inject , ChangeDetectionStrategy} from '@angular/core';
import { DatePipe } from '@angular/common';
import { AppointmentResponse } from '../../../core/models';
import { HebrewCalendarService } from '../../../core/services/hebrew-calendar.service';
import { IconComponent } from '../icon/icon.component';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-cancel-confirm-dialog',
  standalone: true,
  imports: [DatePipe, IconComponent],
  templateUrl: './cancel-confirm-dialog.component.html'
})
export class CancelConfirmDialogComponent {
  @Input() appointment!: AppointmentResponse;
  @Input() question = 'האם לבטל את התור?';
  @Input() cancelling = false;

  @Output() confirmed = new EventEmitter<void>();
  @Output() closed    = new EventEmitter<void>();

  readonly hebrewCalendar = inject(HebrewCalendarService);
}
