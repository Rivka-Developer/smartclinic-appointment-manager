// swap-accept-dialog — אישור קבלת תור מלוח הפנויים.
// מזהיר שהתור בתוך 24 שעות ולא ניתן לביטול.

import { Component, Input, Output, EventEmitter, inject, ChangeDetectionStrategy } from '@angular/core';
import { SwapOfferResponse } from '../../../core/models';
import { HebrewCalendarService } from '../../../core/services/hebrew-calendar.service';
import { IconComponent } from '../icon/icon.component';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-swap-accept-dialog',
  standalone: true,
  imports: [IconComponent],
  templateUrl: './swap-accept-dialog.component.html'
})
export class SwapAcceptDialogComponent {
  @Input() offer!: SwapOfferResponse;
  @Input() accepting = false;

  @Output() confirmed = new EventEmitter<void>();
  @Output() closed    = new EventEmitter<void>();

  readonly hebrewCalendar = inject(HebrewCalendarService);

  formatDate(iso: string): string {
    const d = new Date(iso);
    const dd = d.getDate().toString().padStart(2, '0');
    const mm = (d.getMonth() + 1).toString().padStart(2, '0');
    return `${dd}/${mm}/${d.getFullYear()}`;
  }

  formatTime(iso: string): string {
    return new Date(iso).toLocaleTimeString('he-IL', { hour: '2-digit', minute: '2-digit' });
  }

  getEndTime(iso: string, duration: number): string {
    const end = new Date(new Date(iso).getTime() + duration * 60_000);
    return end.toLocaleTimeString('he-IL', { hour: '2-digit', minute: '2-digit' });
  }
}
