// src/app/features/client/my-appointments/my-appointments.component.ts
// דף "התורים שלי" של הלקוחה.
//
// מציג רשימת כל התורים + כפתור ביטול לתורים מעל 24 שעות.
// תורים שלא ניתן לבטל — כפתור "לא יכולה להגיע? פרסמי" שפותח דיאלוג אישור כללים.

import { Component, DestroyRef, inject, signal, OnInit, ChangeDetectionStrategy, computed } from '@angular/core';
import { DatePipe, NgClass } from '@angular/common';
import { Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AppointmentsService } from '../../../core/services/appointments.service';
import { SwapOffersService } from '../../../core/services/swap-offers.service';
import { SnackService } from '../../../core/services/snack.service';
import { SpinnerComponent } from '../../../shared/components/spinner/spinner.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { CancelConfirmDialogComponent } from '../../../shared/components/cancel-confirm-dialog/cancel-confirm-dialog.component';
import { PublishConfirmDialogComponent } from '../../../shared/components/publish-confirm-dialog/publish-confirm-dialog.component';
import { AppointmentResponse, SwapOfferResponse } from '../../../core/models';
import { HebrewCalendarService } from '../../../core/services/hebrew-calendar.service';
import { statusClass, statusLabel } from '../../../core/utils/appointment-status.utils';
import { performCancel } from '../../../core/utils/signal.utils';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-my-appointments',
  standalone: true,
  imports: [
    DatePipe, NgClass,
    SpinnerComponent, EmptyStateComponent, IconComponent,
    CancelConfirmDialogComponent, PublishConfirmDialogComponent
  ],
  templateUrl: './my-appointments.component.html',
  styleUrls: ['./my-appointments.component.css']
})
export class MyAppointmentsComponent implements OnInit {
  readonly apptService    = inject(AppointmentsService);
  readonly router         = inject(Router);
  private snack           = inject(SnackService);
  private swapService     = inject(SwapOffersService);
  readonly hebrewCalendar = inject(HebrewCalendarService);
  private destroyRef      = inject(DestroyRef);

  /** התור שממתין לאישור ביטול */
  confirmAppt = signal<AppointmentResponse | null>(null);
  cancelling  = signal(false);

  /** התור שממתין לאישור פרסום כתור פנוי */
  publishAppt  = signal<AppointmentResponse | null>(null);
  publishing   = signal(false);

  cancellingOffer = signal(false);

  readonly appointments = computed(() =>
    this.apptService.myAppointments().filter(a => a.status !== 'Cancelled')
  );

  /** מיפוי appointmentId → הצעה פעילה, לזיהוי תורים שכבר פורסמו */
  readonly activeOfferByAppointmentId = computed(() => {
    const map = new Map<string, SwapOfferResponse>();
    for (const offer of this.swapService.activeOffers()) {
      map.set(offer.appointmentId, offer);
    }
    return map;
  });

  getActiveOffer(appt: AppointmentResponse): SwapOfferResponse | undefined {
    return this.activeOfferByAppointmentId().get(appt.id);
  }

  ngOnInit(): void {
    this.apptService.loadMyAppointments();
    this.swapService.loadActiveOffers();
  }

  isPast(appt: AppointmentResponse): boolean {
    return new Date(appt.endTime).getTime() < Date.now();
  }

  canCancel(appt: AppointmentResponse): boolean {
    return new Date(appt.startTime).getTime() - Date.now() > 24 * 60 * 60 * 1000;
  }

  cancel(appt: AppointmentResponse): void {
    this.confirmAppt.set(appt);
  }

  confirmCancel(): void {
    const appt = this.confirmAppt();
    if (!appt) return;
    performCancel(
      this.apptService.cancel(appt.id),
      this.cancelling,
      () => { this.snack.success('התור בוטל בהצלחה'); this.confirmAppt.set(null); this.apptService.loadMyAppointments(); },
      () => this.snack.error('שגיאה בביטול התור'),
      this.destroyRef
    );
  }

  /** פותח דיאלוג אישור כללי הפרסום */
  openPublishDialog(appt: AppointmentResponse): void {
    this.publishAppt.set(appt);
  }

  /** נקרא לאחר אישור הדיאלוג — שולח את ההצעה לשרת */
  confirmPublish(): void {
    const appt = this.publishAppt();
    if (!appt) return;

    this.publishing.set(true);
    this.swapService.createOffer({ appointmentId: appt.id })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.publishing.set(false);
          this.publishAppt.set(null);
          this.swapService.loadActiveOffers();
          this.snack.success('התור פורסם בהצלחה — הוא מופיע עכשיו בתורים פנויים');
        },
        error: (err) => {
          this.publishing.set(false);
          const detail = err?.error?.detail;
          this.snack.error(detail ?? 'שגיאה בפרסום התור');
        }
      });
  }

  /** ביטול פרסום תור — מבטל את ה-offer הפעיל */
  cancelPublish(offerId: string): void {
    this.cancellingOffer.set(true);
    this.swapService.cancelOffer(offerId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.cancellingOffer.set(false);
          this.snack.success('פרסום התור בוטל — התור נשמר אצלך.');
          this.swapService.loadActiveOffers();
        },
        error: () => {
          this.cancellingOffer.set(false);
          this.snack.error('שגיאה בביטול הפרסום');
        }
      });
  }

  holidays(isoStr: string): string[] {
    return this.hebrewCalendar.getDayHolidays(new Date(isoStr));
  }

  readonly statusClass = statusClass;
  readonly statusLabel = statusLabel;
}
