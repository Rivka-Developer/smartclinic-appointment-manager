// src/app/features/client/swap-board/swap-board.component.ts
// לוח העברת תורים — מציג הצעות פעילות בתוך 24 שעות.
// לקוחות יכולות לקחת תור של אחרת, או לבטל הצעה שפרסמו.

import {
  Component, OnInit, ChangeDetectionStrategy,
  inject, signal, computed, DestroyRef
} from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { SwapOffersService } from '../../../core/services/swap-offers.service';
import { AppointmentsService } from '../../../core/services/appointments.service';
import { AuthService } from '../../../core/services/auth.service';
import { SnackService } from '../../../core/services/snack.service';
import { HebrewCalendarService } from '../../../core/services/hebrew-calendar.service';
import { SpinnerComponent } from '../../../shared/components/spinner/spinner.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { SwapOfferResponse } from '../../../core/models';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SwapAcceptDialogComponent } from '../../../shared/components/swap-accept-dialog/swap-accept-dialog.component';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-swap-board',
  standalone: true,
  imports: [SpinnerComponent, EmptyStateComponent, IconComponent, SwapAcceptDialogComponent],
  templateUrl: './swap-board.component.html',
  styleUrls: ['./swap-board.component.css']
})
export class SwapBoardComponent implements OnInit {
  readonly swapService     = inject(SwapOffersService);
  readonly apptService     = inject(AppointmentsService);
  readonly hebrewCalendar  = inject(HebrewCalendarService);
  private  auth            = inject(AuthService);
  private  snack           = inject(SnackService);
  private  route           = inject(ActivatedRoute);
  private  destroyRef      = inject(DestroyRef);

  /** מזהי התורים של הלקוחה המחוברת — לזיהוי הצעות שהיא עצמה פרסמה */
  readonly myAppointmentIds = computed(() =>
    new Set(this.apptService.myAppointments().map(a => a.id))
  );

  /** האם ה-offer שייך ללקוחה המחוברת */
  isOwnOffer(offer: SwapOfferResponse): boolean {
    return this.myAppointmentIds().has(offer.appointmentId);
  }

  /** שם הלקוחה המחוברת */
  private get currentUserName(): string | undefined {
    return this.auth.user()?.fullName;
  }

  accepting    = signal<string | null>(null);
  cancelling   = signal<string | null>(null);
  pendingOffer = signal<SwapOfferResponse | null>(null);

  ngOnInit(): void {
    this.swapService.loadActiveOffers();
    this.apptService.loadMyAppointments();

    // queryParam ?post=<appointmentId> — מגיע מדף "התורים שלי"
    // (תשתית לשיפור עתידי: פתיחת dialog אוטומטי לפרסום הצעה)
    this.route.queryParams
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(params => {
        if (params['post']) {
          this.postOffer(params['post']);
        }
      });
  }

  /** פרסם הצעה (נקרא גם דרך queryParam) */
  postOffer(appointmentId: string): void {
    this.swapService.createOffer({ appointmentId })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.snack.success('ההצעה פורסמה בלוח בהצלחה!');
          this.swapService.loadActiveOffers();
        },
        error: (err) => {
          const detail = err?.error?.detail;
          this.snack.error(detail ?? 'שגיאה בפרסום ההצעה');
        }
      });
  }

  accept(offer: SwapOfferResponse): void {
    this.pendingOffer.set(offer);
  }

  confirmAccept(): void {
    const offer = this.pendingOffer();
    if (!offer) return;
    this.pendingOffer.set(null);
    this.accepting.set(offer.id);
    this.swapService.acceptOffer(offer.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.accepting.set(null);
          this.snack.success('התור עבר אליך בהצלחה!');
          this.swapService.loadActiveOffers();
          this.apptService.loadMyAppointments();
        },
        error: (err) => {
          this.accepting.set(null);
          const detail = err?.error?.detail;
          this.snack.error(detail ?? 'שגיאה בקבלת ההצעה');
        }
      });
  }

  cancelOffer(offer: SwapOfferResponse): void {
    this.cancelling.set(offer.id);
    this.swapService.cancelOffer(offer.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.cancelling.set(null);
          this.snack.success('ההצעה בוטלה — התור נשמר אצלך.');
          this.swapService.loadActiveOffers();
        },
        error: () => {
          this.cancelling.set(null);
          this.snack.error('שגיאה בביטול ההצעה');
        }
      });
  }

  private readonly DAY_NAMES = ['ראשון', 'שני', 'שלישי', 'רביעי', 'חמישי', 'שישי', 'שבת'];

  formatTime(iso: string): string {
    return new Date(iso).toLocaleTimeString('he-IL', { hour: '2-digit', minute: '2-digit' });
  }

  getDayOfWeek(iso: string): string {
    return 'יום ' + this.DAY_NAMES[new Date(iso).getDay()];
  }

  getRelativeDay(iso: string): string {
    const today = new Date(); today.setHours(0, 0, 0, 0);
    const d = new Date(iso); d.setHours(0, 0, 0, 0);
    const diff = Math.round((d.getTime() - today.getTime()) / 86_400_000);
    if (diff === 0) return 'היום';
    if (diff === 1) return 'מחר';
    if (diff === 2) return 'מחרתיים';
    return '';
  }

  getGregorianDate(iso: string): string {
    const d = new Date(iso);
    const dd = d.getDate().toString().padStart(2, '0');
    const mm = (d.getMonth() + 1).toString().padStart(2, '0');
    return `${dd}/${mm}/${d.getFullYear()}`;
  }

  getEndTime(iso: string, duration: number): string {
    const end = new Date(new Date(iso).getTime() + duration * 60_000);
    return end.toLocaleTimeString('he-IL', { hour: '2-digit', minute: '2-digit' });
  }

  getDurationLabel(minutes: number): string {
    if (minutes === 30)  return 'חצי שעה';
    if (minutes === 45)  return '45 דקות';
    if (minutes === 60)  return 'שעה';
    if (minutes === 90)  return 'שעה וחצי';
    if (minutes === 120) return 'שעתיים';
    return `${minutes} דקות`;
  }
}
