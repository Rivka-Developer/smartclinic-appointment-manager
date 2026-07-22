import {
  Component, OnInit, ChangeDetectionStrategy,
  inject, signal, DestroyRef
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { NgClass } from '@angular/common';
import { SwapOffersService } from '../../../core/services/swap-offers.service';
import { AppointmentsService } from '../../../core/services/appointments.service';
import { UsersService } from '../../../core/services/users.service';
import { SnackService } from '../../../core/services/snack.service';
import { SpinnerComponent } from '../../../shared/components/spinner/spinner.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { AdminSwapOfferResponse, AppointmentResponse, SwapOfferStatus } from '../../../core/models';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-swap-management',
  standalone: true,
  imports: [FormsModule, NgClass, SpinnerComponent, EmptyStateComponent, IconComponent],
  templateUrl: './swap-management.component.html',
  styleUrls: ['./swap-management.component.css']
})
export class SwapManagementComponent implements OnInit {
  readonly swapService  = inject(SwapOffersService);
  readonly usersService = inject(UsersService);
  private apptService   = inject(AppointmentsService);
  private snack         = inject(SnackService);
  private destroyRef    = inject(DestroyRef);

  // ── פילטר ──────────────────────────────────────────
  activeFilter = signal<SwapOfferStatus | undefined>(undefined);

  // ── דיאלוג הוספת הצעה ──────────────────────────────
  showAddDialog    = signal(false);
  selectedApptId   = signal('');
  addSubmitting    = signal(false);
  futureAppointments = signal<AppointmentResponse[]>([]);

  // ── דיאלוג קבלת הצעה ───────────────────────────────
  pendingAcceptOffer = signal<AdminSwapOfferResponse | null>(null);
  selectedClientId   = signal('');
  acceptSubmitting   = signal(false);

  // ── סטטוס פעולות מהירות ────────────────────────────
  cancelling = signal<string | null>(null);

  ngOnInit(): void {
    this.swapService.loadAdminOffers();
    this.usersService.loadClients();
  }

  applyFilter(status: SwapOfferStatus | undefined): void {
    this.activeFilter.set(status);
    this.swapService.loadAdminOffers(status);
  }

  // ── הוספת הצעה ─────────────────────────────────────
  openAddDialog(): void {
    const start = new Date();
    const end   = new Date(Date.now() + 24 * 60 * 60 * 1000);
    this.apptService.getAdminCalendar(start, end)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: appts => {
          this.futureAppointments.set(
            appts.filter(a => a.status === 'Scheduled' && new Date(a.startTime) > new Date())
          );
          this.selectedApptId.set('');
          this.showAddDialog.set(true);
        },
        error: () => this.snack.error('שגיאה בטעינת תורים')
      });
  }

  submitAddOffer(): void {
    const id = this.selectedApptId();
    if (!id) return;
    this.addSubmitting.set(true);
    this.swapService.adminCreateOffer(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.addSubmitting.set(false);
          this.showAddDialog.set(false);
          this.snack.success('ההצעה פורסמה בלוח בהצלחה!');
          this.swapService.loadAdminOffers(this.activeFilter());
        },
        error: (err) => {
          this.addSubmitting.set(false);
          this.snack.error(err?.error?.detail ?? 'שגיאה בפרסום ההצעה');
        }
      });
  }

  // ── קבלת הצעה ──────────────────────────────────────
  openAcceptDialog(offer: AdminSwapOfferResponse): void {
    this.selectedClientId.set('');
    this.pendingAcceptOffer.set(offer);
  }

  submitAcceptOffer(): void {
    const offer    = this.pendingAcceptOffer();
    const clientId = this.selectedClientId();
    if (!offer || !clientId) return;
    this.acceptSubmitting.set(true);
    this.swapService.adminAcceptOffer(offer.id, { targetClientId: clientId })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.acceptSubmitting.set(false);
          this.pendingAcceptOffer.set(null);
          this.snack.success('התור הועבר ללקוחה בהצלחה!');
          this.swapService.loadAdminOffers(this.activeFilter());
        },
        error: (err) => {
          this.acceptSubmitting.set(false);
          this.snack.error(err?.error?.detail ?? 'שגיאה בקבלת ההצעה');
        }
      });
  }

  // ── ביטול הצעה ──────────────────────────────────────
  cancelOffer(offer: AdminSwapOfferResponse): void {
    this.cancelling.set(offer.id);
    this.swapService.adminCancelOffer(offer.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.cancelling.set(null);
          this.snack.success('ההצעה בוטלה בהצלחה.');
          this.swapService.loadAdminOffers(this.activeFilter());
        },
        error: () => {
          this.cancelling.set(null);
          this.snack.error('שגיאה בביטול ההצעה');
        }
      });
  }

  // ── עזרים לתצוגה ────────────────────────────────────
  formatTime(iso: string): string {
    return new Date(iso).toLocaleTimeString('he-IL', { hour: '2-digit', minute: '2-digit' });
  }

  formatDate(iso: string): string {
    const d = new Date(iso);
    return `${d.getDate().toString().padStart(2, '0')}/${(d.getMonth() + 1).toString().padStart(2, '0')}/${d.getFullYear()}`;
  }

  getEndTime(iso: string, duration: number): string {
    return new Date(new Date(iso).getTime() + duration * 60_000)
      .toLocaleTimeString('he-IL', { hour: '2-digit', minute: '2-digit' });
  }

  statusLabel(status: SwapOfferStatus): string {
    switch (status) {
      case 'Active':    return 'פעילה';
      case 'Accepted':  return 'התקבלה';
      case 'Cancelled': return 'בוטלה';
    }
  }

  statusClass(status: SwapOfferStatus): string {
    switch (status) {
      case 'Active':    return 'status-active';
      case 'Accepted':  return 'status-accepted';
      case 'Cancelled': return 'status-cancelled';
    }
  }
}
