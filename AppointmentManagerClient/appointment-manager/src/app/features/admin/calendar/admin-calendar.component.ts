import { Component, DestroyRef, inject, signal, computed, OnInit, ViewChild, ChangeDetectionStrategy } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { AppointmentsService } from '../../../core/services/appointments.service';
import { WorkShiftsService } from '../../../core/services/work-shifts.service';
import { SettingsService } from '../../../core/services/settings.service';
import { SnackService } from '../../../core/services/snack.service';
import { AdminBookingStateService } from '../../../core/services/admin-booking-state.service';
import { ShiftDialogComponent } from '../shift-dialog/shift-dialog.component';
import { BookingDialogComponent } from '../../../shared/components/booking-dialog/booking-dialog.component';
import { CancelConfirmDialogComponent } from '../../../shared/components/cancel-confirm-dialog/cancel-confirm-dialog.component';
import { WeekCalendarComponent } from '../../../shared/components/week-calendar/week-calendar.component';
import { WeeklyTemplateComponent } from '../weekly-template/weekly-template.component';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { AppointmentResponse, WorkShiftResponse, TimeBlockDto } from '../../../core/models';
import { formatTime, formatTimeSpan, toMinutes, WeekDayBase, WeekBuiltEvent } from '../../../core/utils/calendar.utils';
import { makeCountdown, performCancel } from '../../../core/utils/signal.utils';

/** נתוני יום בתצוגת המנהלת */
interface AdminDayData extends WeekDayBase {
  loaded: boolean;
  shifts: WorkShiftResponse[];
  appointments: AppointmentResponse[];
  freeBlocks: TimeBlockDto[];
}

/** פריט ממוין בעמודת יום (תור קיים או בלוק פנוי) */
interface SortedItem {
  kind: 'appt' | 'free';
  appt: AppointmentResponse | null;
  block: TimeBlockDto | null;
  startMs: number;
  isEvening: boolean;
  isFirstEvening: boolean;
}

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-admin-calendar',
  standalone: true,
  imports: [DatePipe, WeekCalendarComponent, ShiftDialogComponent, BookingDialogComponent, CancelConfirmDialogComponent, WeeklyTemplateComponent, IconComponent],
  templateUrl: './admin-calendar.component.html',
  styleUrls: ['./admin-calendar.component.css']
})
export class AdminCalendarComponent implements OnInit {
  private apptService        = inject(AppointmentsService);
  private shiftsService      = inject(WorkShiftsService);
  private settingsService    = inject(SettingsService);
  private snack              = inject(SnackService);
  private destroyRef         = inject(DestroyRef);
  protected router           = inject(Router);
  readonly adminBookingState = inject(AdminBookingStateService);

  loading       = signal(false);
  weekDays      = signal<AdminDayData[]>([]);
  currentWeekStart = signal<Date | null>(null);

  readonly eveningStartMinutes = this.settingsService.eveningStartMinutes;

  // ─── מצב דיאלוג משמרות ────────────────────────────────────────────────────
  shiftDialogDay = signal<AdminDayData | null>(null);

  // ─── מצב דיאלוג ביטול ─────────────────────────────────────────────────────
  cancelAppt = signal<AppointmentResponse | null>(null);
  cancelling = signal(false);

  // ─── מצב דיאלוג קביעת תור עבור לקוחה ────────────────────────────────────
  bookDate  = signal<Date | null>(null);
  bookBlock = signal<TimeBlockDto | null>(null);

  // ─── חשיפת פונקציות עזר לתבנית ─────────────────────────────────────────
  readonly formatTime     = formatTime;
  readonly formatTimeSpan = formatTimeSpan;

  readonly hasAnyEveningItems = computed(() =>
    this.weekDays().some(day => this.eveningItems(day).length > 0)
  );

  ngOnInit(): void {
    this.settingsService.load();
  }

  // ─── מגיב לבניית שבוע מה-WeekCalendarComponent ───────────────────────────
  onWeekBuilt(event: WeekBuiltEvent): void {
    this.currentWeekStart.set(event.weekStart);
    const days: AdminDayData[] = event.days.map(d => ({
      ...d, loaded: false, shifts: [], appointments: [], freeBlocks: []
    }));
    this.weekDays.set(days);
    this.loading.set(true);
    const done = makeCountdown(8, () => this.loading.set(false)); // 1 (getAdminCalendar) + 7 (getClientView)

    this.apptService.getAdminCalendar(event.weekStart, event.weekEnd).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: appts => {
        this.weekDays.update(d => {
          appts.forEach(appt => {
            const idx = d.findIndex(day => day.date.toDateString() === new Date(appt.startTime).toDateString());
            if (idx >= 0) d[idx].appointments.push(appt);
          });
          return [...d];
        });
      },
      error: done, complete: done
    });

    days.forEach((day, i) => {
      if (day.isWeekend) {
        // סופ"ש — לא מציגים משמרות או זמינות
        this.weekDays.update(d => { d[i].loaded = true; return [...d]; });
        done();
        return;
      }

      this.shiftsService.getByDate(day.date).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: shifts => { this.weekDays.update(d => { d[i].shifts = shifts; d[i].loaded = true; return [...d]; }); },
        error: ()     => { this.weekDays.update(d => { d[i].loaded = true; return [...d]; }); }
      });

      this.apptService.getClientView(day.date).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: res => { this.weekDays.update(d => { d[i].freeBlocks = res.freeBlocks ?? []; d[i].loaded = true; return [...d]; }); },
        error: ()  => { this.weekDays.update(d => { d[i].loaded = true; return [...d]; }); done(); },
        complete: done
      });
    });
  }

  // ─── דיאלוג משמרות ────────────────────────────────────────────────────────
  onDayHeaderClick(e: { day: WeekDayBase; index: number }): void {
    if (!e.day.isWeekend) {
      this.shiftDialogDay.set(this.weekDays()[e.index]);
    }
  }

  @ViewChild(WeekCalendarComponent) weekCal!: WeekCalendarComponent;

  onShiftChanged(): void {
    this.shiftDialogDay.set(null);
    this.weekCal.rebuildWeek();
  }

  // ─── דיאלוג ביטול ─────────────────────────────────────────────────────────
  openCancelDialog(appt: AppointmentResponse): void {
    if (appt.status === 'Cancelled') return;
    this.cancelAppt.set(appt);
  }

  doCancel(): void {
    const appt = this.cancelAppt();
    if (!appt) return;
    performCancel(
      this.apptService.cancel(appt.id),
      this.cancelling,
      () => { this.snack.success('התור בוטל'); this.cancelAppt.set(null); this.weekCal.rebuildWeek(); },
      () => this.snack.error('שגיאה בביטול'),
      this.destroyRef
    );
  }

  // ─── דיאלוג קביעת תור ─────────────────────────────────────────────────────
  openBookForClientDialog(date: Date, block: TimeBlockDto): void {
    this.bookDate.set(date);
    this.bookBlock.set(block);
  }

  onAdminBooked(): void {
    this.adminBookingState.clear();
    this.bookDate.set(null);
    this.bookBlock.set(null);
    this.weekCal.rebuildWeek();
  }

  cancelPendingBooking(): void {
    this.adminBookingState.clear();
    this.router.navigate(['/admin/clients']);
  }

  // ─── עיבוד פריטים לתצוגה ──────────────────────────────────────────────────

  sortedItems(day: AdminDayData): SortedItem[] {
    const evMin = this.eveningStartMinutes();
    const raw: Omit<SortedItem, 'isEvening' | 'isFirstEvening'>[] = [
      ...day.appointments.map(a => ({ kind: 'appt' as const, appt: a, block: null, startMs: toMinutes(a.startTime) })),
      ...day.freeBlocks.map(b => ({ kind: 'free' as const, appt: null, block: b, startMs: toMinutes(b.start) }))
    ];
    raw.sort((a, b) => {
      if (a.startMs !== b.startMs) return a.startMs - b.startMs;
      return a.kind === b.kind ? 0 : a.kind === 'appt' ? -1 : 1;
    });
    let foundFirstEvening = false;
    return raw.map(item => {
      const isEvening = item.startMs >= evMin;
      const isFirstEvening = isEvening && !foundFirstEvening;
      if (isFirstEvening) foundFirstEvening = true;
      return { ...item, isEvening, isFirstEvening };
    });
  }

  morningItems(day: AdminDayData): SortedItem[] {
    return this.sortedItems(day).filter(item => !item.isEvening);
  }

  eveningItems(day: AdminDayData): SortedItem[] {
    return this.sortedItems(day).filter(item => item.isEvening);
  }

  isEveningTime(ts: string): boolean {
    return toMinutes(ts) >= this.eveningStartMinutes();
  }

  toAppt(item: SortedItem): AppointmentResponse { return item.appt as AppointmentResponse; }
  toBlock(item: SortedItem): TimeBlockDto       { return item.block as TimeBlockDto; }
}
