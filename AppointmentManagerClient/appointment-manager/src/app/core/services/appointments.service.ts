// src/app/core/services/appointments.service.ts
// ─────────────────────────────────────────────────────────────────────────────
// שירות ניהול תורים.
//
// אחראי על כל התקשורת עם ה-API הקשורה לתורים:
//   - קבלת זמנים פנויים לתאריך
//   - קבלת אפשרויות מיקום תור
//   - קביעת תורים (ללקוח עצמו ולמנהלת עבור לקוח)
//   - שליפת היסטוריית תורים של לקוח
//   - ביטול תור
//   - שליפת תורים לצורך לוח שנה של מנהלת
//
// כולל Signals לשמירת המצב המשותף בין קומפוננטות.
// ─────────────────────────────────────────────────────────────────────────────

import { Injectable, signal, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  AppointmentRequest,
  AppointmentResponse,
  AvailableSlotsResponse,
  PlacementOptionsResponse
} from '../models';
import { environment } from '../../../environments/environment';
import { toLocalDateStr, toLocalISOStr } from '../utils/calendar.utils';
import { loadSignal } from '../utils/signal.utils';

@Injectable({ providedIn: 'root' }) // singleton – נוצר פעם אחת לכל האפליקציה
export class AppointmentsService {
  private http = inject(HttpClient);  // לשליחת בקשות HTTP

  /** כתובת הבסיס של נקודות הקצה לתורים */
  private readonly BASE = `${environment.apiUrl}/appointments`;

  // ─── Signals (מצב משותף) ─────────────────────────────────────────────────

  /** רשימת התורים של הלקוח המחובר – מתעדכן בעת קריאה ל-loadMyAppointments() */
  readonly myAppointments = signal<AppointmentResponse[]>([]);

  /** האם מתבצעת כרגע בקשת HTTP? – לשליטה בתצוגת spinner */
  readonly loading = signal(false);

  /** תורים לצורך לוח שנה של מנהלת – מתעדכן בעת קריאה ל-loadAdminCalendar() */
  readonly adminCalendar = signal<AppointmentResponse[]>([]);

  // ─── נקודות קצה של לקוח ──────────────────────────────────────────────────

  /**
   * מקבל את הזמנים הפנויים ביום מסוים מנקודת המבט של הלקוח.
   * (זהה ל-getAvailableSlots, קיים לאחידות בשמות בין הדפים)
   *
   * @param date – התאריך הרצוי
   * @returns Observable עם רשימת הבלוקים הפנויים
   */
  getClientView(date: Date): Observable<AvailableSlotsResponse> {
    // HttpParams בונה query string: ?date=YYYY-MM-DD
    const params = new HttpParams().set('date', toLocalDateStr(date));
    return this.http.get<AvailableSlotsResponse>(`${this.BASE}/available-slots`, { params });
  }

  /**
   * מקבל אפשרויות מיקום ספציפיות לבלוק מסוים שנבחר.
   * GET /appointments/block-placement-options?date=...&durationMinutes=...&selectedBlockStart=...
   *
   * @param date               – תאריך התור
   * @param durationMinutes    – משך התור בדקות
   * @param selectedBlockStart – שעת תחילת הבלוק שנבחר (פורמט "HH:mm")
   * @returns Observable עם אפשרויות המיקום לבלוק הספציפי
   */
  getBlockPlacementOptions(date: Date, durationMinutes: number, selectedBlockStart: string): Observable<PlacementOptionsResponse> {
    const params = new HttpParams()
      .set('date', toLocalDateStr(date))
      .set('durationMinutes', durationMinutes)
      .set('selectedBlockStart', selectedBlockStart); // מיקום הבלוק לחיפוש מדויק
    return this.http.get<PlacementOptionsResponse>(`${this.BASE}/block-placement-options`, { params });
  }

  /**
   * קובע תור עבור הלקוח המחובר.
   * POST /appointments/book
   * השרת שולף את פרטי הלקוח מה-JWT token.
   *
   * @param req – פרטי התור (זמן, משך; שם וטלפון ריקים כי הם מה-token)
   * @returns Observable עם הודעת אישור
   */
  book(req: AppointmentRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.BASE}/book`, req);
  }

  /**
   * קובע תור עבור לקוח ספציפי (בשימוש מנהלת).
   * POST /appointments/book-for-client
   * המנהלת מספקת שם וטלפון ידנית.
   *
   * @param req – פרטי התור כולל שם וטלפון הלקוח
   * @returns Observable עם הודעת אישור
   */
  bookForClient(req: AppointmentRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.BASE}/book-for-client`, req);
  }

  /**
   * מקבל את היסטוריית התורים של הלקוח המחובר.
   * GET /appointments/my-history
   *
   * @returns Observable עם רשימת כל התורים (עבר ועתיד)
   */
  getMyHistory(): Observable<AppointmentResponse[]> {
    return this.http.get<AppointmentResponse[]>(`${this.BASE}/my-history`);
  }

  /**
   * טוען את תורי הלקוח ושומר אותם ב-Signal myAppointments.
   * מנהל loading state לתצוגת spinner.
   * נקרא מ-MyAppointmentsComponent ב-ngOnInit.
   */
  loadMyAppointments(): void {
    loadSignal(this.getMyHistory(), this.myAppointments, this.loading);
  }

  // ─── ביטול תור (משותף ללקוח ומנהלת) ────────────────────────────────────

  /**
   * מבטל תור לפי מזהה.
   * DELETE /appointments/:id
   *
   * @param id – מזהה ייחודי של התור
   * @returns Observable ריק (void)
   */
  cancel(id: string): Observable<void> {
    return this.http.delete<void>(`${this.BASE}/${id}`);
  }

  // ─── נקודות קצה של מנהלת ────────────────────────────────────────────────

  /**
   * מקבל את כל התורים בטווח תאריכים לצורך לוח שנה של מנהלת.
   * GET /appointments/admin-calendar?start=...&end=...
   *
   * @param start – תחילת הטווח (ISO datetime)
   * @param end   – סוף הטווח (ISO datetime)
   * @returns Observable עם רשימת כל התורים בטווח
   */
  getAdminCalendar(start: Date, end: Date): Observable<AppointmentResponse[]> {
    const params = new HttpParams()
      .set('start', toLocalISOStr(start)) // פורמט ISO ללא timezone
      .set('end', toLocalISOStr(end));
    return this.http.get<AppointmentResponse[]>(`${this.BASE}/admin-calendar`, { params });
  }

  /**
   * טוען את לוח השנה של המנהלת ושומר ב-Signal adminCalendar.
   *
   * @param start – תחילת השבוע
   * @param end   – סוף השבוע
   */
  loadAdminCalendar(start: Date, end: Date): void {
    loadSignal(this.getAdminCalendar(start, end), this.adminCalendar, this.loading);
  }
}
