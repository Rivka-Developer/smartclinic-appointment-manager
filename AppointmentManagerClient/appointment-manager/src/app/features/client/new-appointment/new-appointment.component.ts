import { Component, inject, signal, computed, OnInit, ViewChild, DestroyRef, ChangeDetectionStrategy } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AppointmentsService } from '../../../core/services/appointments.service';
import { SettingsService } from '../../../core/services/settings.service';
import { SnackService } from '../../../core/services/snack.service';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { BookingDialogComponent } from '../../../shared/components/booking-dialog/booking-dialog.component';
import { WeekCalendarComponent } from '../../../shared/components/week-calendar/week-calendar.component';
import { CancelConfirmDialogComponent } from '../../../shared/components/cancel-confirm-dialog/cancel-confirm-dialog.component';
import { TimeBlockDto, AppointmentResponse } from '../../../core/models';
import { formatTime, toMinutes, WeekDayBase, WeekBuiltEvent } from '../../../core/utils/calendar.utils';
import { makeCountdown, performCancel } from '../../../core/utils/signal.utils';

/** נתוני יום בלוח השנה */
interface DayData extends WeekDayBase {
  freeBlocks: TimeBlockDto[];
  loaded: boolean;
}

/** פריט מאוחד בלוח: בלוק פנוי או תור קיים של הלקוחה */
type CalendarItem =
  | { kind: 'free'; block: TimeBlockDto; isEvening: boolean; startMinutes: number }
  | { kind: 'mine'; appt: AppointmentResponse; isEvening: boolean; startMinutes: number };

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-new-appointment',
  standalone: true,
  imports: [WeekCalendarComponent, EmptyStateComponent, BookingDialogComponent, CancelConfirmDialogComponent],
  templateUrl: './new-appointment.component.html',
  styleUrls: ['./new-appointment.component.css']
})
export class NewAppointmentComponent implements OnInit {
  private apptService     = inject(AppointmentsService);
  private settingsService = inject(SettingsService);
  private snack           = inject(SnackService);
  private destroyRef      = inject(DestroyRef);

  @ViewChild(WeekCalendarComponent) private weekCal!: WeekCalendarComponent;

  loading    = signal(false);
  weekDays   = signal<DayData[]>([]);
  showDialog = signal(false);
  selectedDate  = signal<Date | null>(null);
  selectedBlock = signal<TimeBlockDto | null>(null);

  confirmAppt = signal<AppointmentResponse | null>(null);
  cancelling  = signal(false);

  readonly eveningStartMinutes = this.settingsService.eveningStartMinutes;
  readonly MAX_WEEKS = 8;

  readonly hasAnyEveningBlocks = computed(() =>
    this.weekDays().some(day => this.eveningItems(day).length > 0)
  );

  allEmpty = computed(() =>
    this.weekDays().filter(d => !d.isWeekend).every(d => d.loaded && d.freeBlocks.length === 0)
  );

  readonly formatTime = formatTime;

  ngOnInit(): void {
    this.settingsService.load();
    this.apptService.loadMyAppointments();
  }

  // ─── מגיב לבניית שבוע מה-WeekCalendarComponent ───────────────────────────
  onWeekBuilt(event: WeekBuiltEvent): void {
    const days: DayData[] = event.days.map(d => ({ ...d, freeBlocks: [], loaded: false }));
    this.weekDays.set(days);
    this.loading.set(true);
    const done = makeCountdown(7, () => this.loading.set(false));

    days.forEach((day, i) => {
      if (day.isPast || day.isWeekend) {
        this.weekDays.update(d => { d[i].loaded = true; return [...d]; });
        done();
      } else {
        this.apptService.getClientView(day.date).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
          next: res => { this.weekDays.update(d => { d[i].freeBlocks = res.freeBlocks ?? []; d[i].loaded = true; return [...d]; }); },
          error: ()  => { this.weekDays.update(d => { d[i].loaded = true; return [...d]; }); done(); },
          complete: done
        });
      }
    });
  }

  // ─── דיאלוג ───────────────────────────────────────────────────────────────
  selectBlock(date: Date, block: TimeBlockDto): void {
    this.selectedDate.set(date);
    this.selectedBlock.set(block);
    this.showDialog.set(true);
  }

  onBooked(): void {
    this.showDialog.set(false);
    this.weekCal.rebuildWeek();
  }

  // ─── ביטול תור ────────────────────────────────────────────────────────────
  cancelAppt(appt: AppointmentResponse): void {
    if (new Date(appt.startTime).getTime() - Date.now() <= 24 * 60 * 60 * 1000) {
      this.snack.error('לא ניתן לבטל תור פחות מ-24 שעות לפני מועדו');
      return;
    }
    this.confirmAppt.set(appt);
  }

  confirmCancel(): void {
    const appt = this.confirmAppt();
    if (!appt) return;
    performCancel(
      this.apptService.cancel(appt.id),
      this.cancelling,
      () => {
        this.snack.success('התור בוטל בהצלחה');
        this.confirmAppt.set(null);
        this.apptService.loadMyAppointments();
        this.weekCal.rebuildWeek();
      },
      () => this.snack.error('שגיאה בביטול התור'),
      this.destroyRef
    );
  }

  // ─── עיבוד פריטים לתצוגה ─────────────────────────────────────────────────
  isDateBlocked(date: Date): boolean {
    const cutoff = new Date(date);
    cutoff.setDate(cutoff.getDate() - 1);
    cutoff.setHours(23, 0, 0, 0);
    return new Date() >= cutoff;
  }

  /** רשימה מאוחדת של בלוקים פנויים + תורים קיימים, ממוינת לפי שעת התחלה */
  calendarItemsForDay(day: DayData): CalendarItem[] {
    const evMin = this.eveningStartMinutes();
    const now = new Date();

    const freeItems: CalendarItem[] = (day.isPast || day.isWeekend || this.isDateBlocked(day.date))
      ? []
      : day.freeBlocks.map(block => ({
          kind: 'free' as const,
          block,
          isEvening: toMinutes(block.start) >= evMin,
          startMinutes: toMinutes(block.start)
        }));

    const myItems: CalendarItem[] = this.apptService.myAppointments()
      .filter(a => {
        if (a.status === 'Cancelled' || new Date(a.endTime) <= now) return false;
        return new Date(a.startTime).toDateString() === day.date.toDateString();
      })
      .map(appt => ({
        kind: 'mine' as const,
        appt,
        isEvening: toMinutes(appt.startTime) >= evMin,
        startMinutes: toMinutes(appt.startTime)
      }));

    return [...freeItems, ...myItems].sort((a, b) => a.startMinutes - b.startMinutes);
  }

  morningItems(day: DayData): CalendarItem[] {
    return this.calendarItemsForDay(day).filter(item => !item.isEvening);
  }

  eveningItems(day: DayData): CalendarItem[] {
    return this.calendarItemsForDay(day).filter(item => item.isEvening);
  }
}
