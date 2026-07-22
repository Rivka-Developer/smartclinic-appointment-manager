// src/app/shared/components/publish-confirm-dialog/publish-confirm-dialog.component.ts
// דיאלוג אישור פרסום תור פנוי.
// מציג את פרטי התור + כללי הפרסום (מי שתיקח — לא ניתן להגיע; אם אף אחת לא תיקח — קנס 50%).
// @Input  appointment – התור לפרסום
// @Input  publishing  – האם הפרסום בתהליך
// @Output confirmed   – נפלט בלחיצת אישור
// @Output closed      – נפלט בסגירה

import { Component, Input, Output, EventEmitter, inject, ChangeDetectionStrategy } from '@angular/core';
import { DatePipe } from '@angular/common';
import { AppointmentResponse } from '../../../core/models';
import { HebrewCalendarService } from '../../../core/services/hebrew-calendar.service';
import { IconComponent } from '../icon/icon.component';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-publish-confirm-dialog',
  standalone: true,
  imports: [DatePipe, IconComponent],
  templateUrl: './publish-confirm-dialog.component.html'
})
export class PublishConfirmDialogComponent {
  @Input() appointment!: AppointmentResponse;
  @Input() publishing = false;

  @Output() confirmed = new EventEmitter<void>();
  @Output() closed    = new EventEmitter<void>();

  readonly hebrewCalendar = inject(HebrewCalendarService);
}
