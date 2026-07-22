// src/app/shared/components/booking-dialog/booking-dialog.component.ts
// ─────────────────────────────────────────────────────────────────────────────
// דיאלוג קביעת תור – משותף ללקוחה ולמנהלת.
//
// כשאין isAdmin (לקוחה): מציג תיאור בלוק, בחירת משך ומיקום, שולח POST /book.
// כשיש isAdmin=true (מנהלת): מוסיף שדות שם וטלפון, שולח POST /book-for-client.
//
// @Input  date     – התאריך שנבחר
// @Input  block    – הבלוק הפנוי שנבחר
// @Input  isAdmin  – האם מצב מנהלת (ברירת מחדל: false)
// @Output booked   – נפלט לאחר קביעה מוצלחת
// @Output closed   – נפלט לסגירת הדיאלוג
// ─────────────────────────────────────────────────────────────────────────────

import { Component, Input, Output, EventEmitter, inject, signal, computed, DestroyRef, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { AppointmentsService } from '../../../core/services/appointments.service';
import { SnackService } from '../../../core/services/snack.service';
import { IconComponent } from '../icon/icon.component';
import { TimeBlockDto, BlockPlacementOptions } from '../../../core/models';
import { formatTime, formatTimeSpan, extractTimeString, toMinutes, toLocalISOStr } from '../../../core/utils/calendar.utils';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-booking-dialog',
  standalone: true,
  imports: [FormsModule, IconComponent],
  templateUrl: './booking-dialog.component.html',
  styleUrls: ['./booking-dialog.component.css']
})
export class BookingDialogComponent implements OnInit {
  /** התאריך שעבורו קובעים תור */
  @Input() date!: Date;

  /** הבלוק הפנוי שנבחר */
  @Input() block!: TimeBlockDto;

  /** האם מצב מנהלת – מציג שדות שם/טלפון ושולח ל-book-for-client */
  @Input() isAdmin = false;

  /** שם לקוחה מולא מראש (כשמגיעים מעמוד הלקוחות) */
  @Input() prefilledName = '';

  /** טלפון לקוחה מולא מראש (כשמגיעים מעמוד הלקוחות) */
  @Input() prefilledPhone = '';

  @Output() booked = new EventEmitter<void>();
  @Output() closed = new EventEmitter<void>();

  private apptService = inject(AppointmentsService);
  private snack       = inject(SnackService);
  private destroyRef  = inject(DestroyRef);

  // ─── מצב פנימי ──────────────────────────────────────────────────────────
  selectedDuration    = signal<number | null>(null);
  placement           = signal<BlockPlacementOptions | null>(null);
  placementChoice     = signal<'start' | 'end' | 'other' | null>(null);
  customTime          = signal('');
  clientName          = signal('');
  clientPhone         = signal('');
  loadingPlacement    = signal(false);
  booking             = signal(false);

  // ─── חשיפת פונקציות עזר לתבנית ─────────────────────────────────────────
  readonly formatTime     = formatTime;
  readonly formatTimeSpan = formatTimeSpan;

  ngOnInit(): void {
    if (this.prefilledName)  this.clientName.set(this.prefilledName);
    if (this.prefilledPhone) this.clientPhone.set(this.prefilledPhone);
  }

  private getErrorMessage(err: HttpErrorResponse): string {
    const code: string | undefined = err.error?.code ?? err.error?.errors?.code?.[0];
    const map: Record<string, string> = {
      'Appointment.NoSlot':        'פרק הזמן שנבחר אינו זמין',
      'Appointment.TooLong':       'משך התור חורג מהזמן המקסימלי המותר',
      'Appointment.PastDate':      'לא ניתן לקבוע תור בתאריך שעבר',
      'Appointment.CannotCancel':  'מועד הביטול עבר',
      'Appointment.SmallGap':      'הבחירה משאירה חור קטן מדי ביומן',
      'Appointment.InvalidInterval': 'הזמן חייב להיות בכפולות של 5 דקות',
      'Appointment.LateNightCutoff': 'ההזמנות ליום זה נסגרו. לפרטים פני/ה למרפאה',
      'Auth.Unauthorized':         'אין הרשאה לבצע פעולה זו',
    };
    return (code && map[code]) ?? 'שגיאה בקביעת התור, אנא נסה שנית';
  }

  // ─── Getters מבוססי @Input (לא computed כי @Input אינו signal) ────────

  get selectedBlockLabel(): string {
    const b = this.block;
    if (!b) return '';
    return `${formatTime(b.start)} – ${formatTime(b.end)} (${b.durationMinutes} דקות)`;
  }

  /** אפשרויות משך: מ-validDurations אם קיים, אחרת 5, 10, 15... עד מקסימום */
  get durationOptions(): number[] {
    const block = this.block;
    if (!block) return [];
    if (block.validDurations?.length) return block.validDurations;
    const max = block.maxAllowedDuration > 0
      ? Math.min(block.durationMinutes, block.maxAllowedDuration)
      : block.durationMinutes;
    const opts: number[] = [];
    for (let m = 5; m <= max; m += 5) opts.push(m);
    return opts;
  }

  /** הודעת מגבלת מקסימום – null אם אין הגבלה */
  get maxHint(): string | null {
    const block = this.block;
    if (!block || !block.maxAllowedDuration || block.maxAllowedDuration >= block.durationMinutes) return null;
    return `מקסימום ${block.maxAllowedDuration} דקות למשמרת זו`;
  }

  // ─── Computed Signals (תלויים במצב פנימי בלבד) ──────────────────────

  /** ולידציה לשעה ידנית: בדיקת כפולות 5 ותחום חוקי */
  customTimeError = computed(() => {
    if (this.placementChoice() !== 'other' || !this.customTime()) return null;
    const p = this.placement();
    if (!p || p.validOtherRanges.length === 0) return 'אין טווחים חוקיים';
    const custom = toMinutes(this.customTime());
    if (custom % 5 !== 0) return 'יש לבחור שעה בכפולות של 5 דקות (למשל 10:00, 10:05, 10:10).';
    const valid = p.validOtherRanges.some(r => custom >= toMinutes(r.start) && custom <= toMinutes(r.end));
    return valid ? null : 'שעה זו יוצרת מרווח לא חוקי (פחות מ-15 דקות). אנא בחרי שעה אחרת.';
  });

  /** האם ניתן לשלוח? בודק כל שדות החובה */
  canBook = computed(() => {
    if (!this.selectedDuration() || !this.placementChoice()) return false;
    if (this.isAdmin && (!this.clientName() || !this.clientPhone())) return false;
    if (this.placementChoice() === 'other') {
      if (!this.customTime()) return false;
      if (this.customTimeError()) return false;
    }
    return true;
  });

  // ─── פעולות ──────────────────────────────────────────────────────────────

  /** טוען אפשרויות מיקום מה-API לבלוק + משך שנבחרו */
  selectDuration(d: number): void {
    this.selectedDuration.set(d);
    this.placementChoice.set(null);
    this.placement.set(null);
    this.customTime.set('');
    this.loadingPlacement.set(true);
    this.apptService.getBlockPlacementOptions(this.date, d, extractTimeString(this.block.start)).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: p => { this.placement.set(p.availableBlocks?.[0] ?? null); this.loadingPlacement.set(false); },
      error: () => { this.snack.error('שגיאה בטעינת אפשרויות מיקום'); this.loadingPlacement.set(false); }
    });
  }

  /** שולח בקשת קביעת תור לפי סוג המשתמש */
  book(): void {
    const choice = this.placementChoice()!;
    const p      = this.placement()!;

    let dt: Date;
    if (choice === 'start') {
      const [h, m] = p.stickToStart.split(':').map(Number);
      dt = new Date(this.date); dt.setHours(h, m, 0, 0);
    } else if (choice === 'end') {
      const [h, m] = p.stickToEnd.split(':').map(Number);
      dt = new Date(this.date); dt.setHours(h, m, 0, 0);
    } else {
      const [h, m] = this.customTime().split(':').map(Number);
      dt = new Date(this.date); dt.setHours(h, m, 0, 0);
    }

    const req = {
      startTime: toLocalISOStr(dt),
      durationMinutes: this.selectedDuration()!,
      clientName:  this.isAdmin ? this.clientName()  : '',
      clientPhone: this.isAdmin ? this.clientPhone().replace(/[-\s]/g, '') : ''
    };

    this.booking.set(true);
    const api$ = this.isAdmin ? this.apptService.bookForClient(req) : this.apptService.book(req);
    api$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.snack.success('התור נקבע בהצלחה!');
        this.booking.set(false);
        this.booked.emit();
      },
      error: (err: HttpErrorResponse) => {
        this.snack.error(this.getErrorMessage(err));
        this.booking.set(false);
      }
    });
  }
}
