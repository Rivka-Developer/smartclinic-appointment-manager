// src/app/features/admin/clients/clients.component.ts
// ─────────────────────────────────────────────────────────────────────────────
// עמוד ניהול לקוחות של המנהלת.
//
// מציג:
//   1. טבלת כל הלקוחות הרשומות עם שם, טלפון, מייל וסה"כ תורים
//   2. תיבת חיפוש בזמן אמת לפי שם או טלפון
//   3. דיאלוג היסטוריית תורים ללקוחה נבחרת
//   4. דיאלוג אישור ביטול תור מתוך ההיסטוריה
// ─────────────────────────────────────────────────────────────────────────────

import { Component, DestroyRef, inject, signal, computed, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe, NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { UsersService } from '../../../core/services/users.service';
import { AppointmentsService } from '../../../core/services/appointments.service';
import { SnackService } from '../../../core/services/snack.service';
import { AdminBookingStateService } from '../../../core/services/admin-booking-state.service';
import { SpinnerComponent } from '../../../shared/components/spinner/spinner.component';
import { CancelConfirmDialogComponent } from '../../../shared/components/cancel-confirm-dialog/cancel-confirm-dialog.component';
import { UserHistoryResponse, AppointmentResponse, UserResponse } from '../../../core/models';
import { HebrewCalendarService } from '../../../core/services/hebrew-calendar.service';
import { statusClass, statusLabel } from '../../../core/utils/appointment-status.utils';
import { performCancel } from '../../../core/utils/signal.utils';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-clients',
  standalone: true,
  imports: [DatePipe, NgClass, FormsModule, SpinnerComponent, IconComponent, CancelConfirmDialogComponent],
  templateUrl: './clients.component.html',
  styleUrls: ['./clients.component.css']
})
export class ClientsComponent implements OnInit {
  /** חשוף כ-readonly כדי שהתבנית תוכל לגשת ל-clients() ו-loading() ישירות */
  readonly usersService      = inject(UsersService);
  private apptService        = inject(AppointmentsService);
  private snack              = inject(SnackService);
  readonly hebrewCalendar    = inject(HebrewCalendarService);
  private destroyRef         = inject(DestroyRef);
  private router             = inject(Router);
  private adminBookingState  = inject(AdminBookingStateService);

  /** טקסט החיפוש הנוכחי – מעודכן בזמן הקלדה */
  searchQuery = signal('');

  /**
   * הלקוחה שהיסטוריית התורים שלה מוצגת בדיאלוג.
   * null = הדיאלוג סגור.
   */
  historyClient = signal<UserHistoryResponse | null>(null);

  /** האם ההיסטוריה נטענת כעת מה-API? */
  historyLoading = signal(false);

  /** האם בקשת הביטול בתהליך? */
  cancelling = signal(false);

  /** התור שממתין לאישור ביטול; null = דיאלוג הביטול סגור */
  confirmAppt = signal<AppointmentResponse | null>(null);

  /**
   * רשימת הלקוחות לאחר סינון לפי searchQuery.
   * מסנן לפי שם (toLowerCase) או מספר טלפון.
   * ריק = מחזיר את כל הרשימה.
   */
  filteredClients = computed(() => {
    const q = this.searchQuery().toLowerCase();
    if (!q) return this.usersService.clients();
    return this.usersService.clients().filter(c =>
      c.fullName.toLowerCase().includes(q) || c.phoneNumber.includes(q)
    );
  });

  /** הלקוח שממתין לאישור מחיקה; null = דיאלוג המחיקה סגור */
  confirmDeleteClient = signal<UserResponse | null>(null);

  /** האם בקשת המחיקה בתהליך? */
  deleting = signal(false);

  /** טוען את רשימת הלקוחות בעת אתחול הקומפוננטה */
  ngOnInit(): void { this.usersService.loadClients(); }

  /**
   * טוען היסטוריית תורים ללקוחה לפי ID ופותח את דיאלוג ההיסטוריה.
   * מציג ספינר בזמן הטעינה.
   */
  loadHistory(id: string): void {
    this.historyLoading.set(true);
    this.usersService.getClientHistory(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: h => { this.historyClient.set(h); this.historyLoading.set(false); },
      error: () => { this.snack.error('שגיאה בטעינת היסטוריה'); this.historyLoading.set(false); }
    });
  }

  /** מציב את התור ב-confirmAppt ופותח את דיאלוג אישור הביטול */
  startCancel(appt: AppointmentResponse): void {
    this.confirmAppt.set(appt);
  }

  /**
   * מבצע ביטול תור לאחר אישור המשתמשת.
   * לאחר הצלחה: מסגר הדיאלוג, מרענן את ההיסטוריה להצגת הסטטוס החדש.
   */
  confirmCancel(): void {
    const appt = this.confirmAppt();
    if (!appt) return;
    performCancel(
      this.apptService.cancel(appt.id),
      this.cancelling,
      () => {
        this.snack.success('התור בוטל בהצלחה');
        this.confirmAppt.set(null);
        const client = this.historyClient();
        if (client) this.loadHistory(client.id);
      },
      () => this.snack.error('שגיאה בביטול'),
      this.destroyRef
    );
  }

  /** פותח את דיאלוג אישור המחיקה עבור לקוח מסוים */
  startDelete(client: UserResponse): void {
    this.confirmDeleteClient.set(client);
  }

  /** מבצע מחיקת לקוח לאחר אישור — מוחק גם את כל התורים שלו */
  confirmDelete(): void {
    const client = this.confirmDeleteClient();
    if (!client || this.deleting()) return;

    this.deleting.set(true);
    this.usersService.deleteUser(client.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.snack.success(`${client.fullName} נמחק/ה בהצלחה`);
        this.confirmDeleteClient.set(null);
        this.deleting.set(false);
        this.usersService.loadClients();
      },
      error: () => {
        this.snack.error('שגיאה במחיקת הלקוח');
        this.deleting.set(false);
      }
    });
  }

  /** מגדיר לקוחה לקביעת תור ומנווט ליומן */
  bookFor(client: UserResponse): void {
    this.adminBookingState.set(client.fullName, client.phoneNumber);
    this.router.navigate(['/admin/calendar']);
  }

  readonly statusClass = statusClass;
  readonly statusLabel = statusLabel;

  isPast(startTime: string): boolean {
    return new Date(startTime) < new Date();
  }
}
