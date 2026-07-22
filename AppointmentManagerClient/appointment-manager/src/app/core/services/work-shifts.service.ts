// src/app/core/services/work-shifts.service.ts
// ─────────────────────────────────────────────────────────────────────────────
// שירות ניהול משמרות עבודה.
//
// אחראי על:
//   - שליפת משמרות לפי תאריך
//   - הוספת משמרת חדשה
//   - עדכון משמרת קיימת
//   - מחיקת משמרת
//
// משמרת = פרק הזמן שבו הקליניקה פתוחה ביום מסוים (למשל 09:30–15:00).
// ייתכנו מספר משמרות ביום (בוקר + ערב).
// ─────────────────────────────────────────────────────────────────────────────

import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { WorkShiftRequest, WorkShiftResponse } from '../models';
import { environment } from '../../../environments/environment';
import { toLocalDateStr } from '../utils/calendar.utils';
import { loadSignal } from '../utils/signal.utils';

@Injectable({ providedIn: 'root' })
export class WorkShiftsService {
  private http = inject(HttpClient);

  /** כתובת הבסיס של נקודות הקצה למשמרות */
  private readonly BASE = `${environment.apiUrl}/workshifts`;

  /** משמרות היום הנוכחי שנטענו אחרונות – Signal לגישה תגובתית */
  readonly shifts = signal<WorkShiftResponse[]>([]);

  /** האם מתבצעת כרגע בקשת HTTP? */
  readonly loading = signal(false);

  /**
   * מקבל את משמרות העבודה של תאריך מסוים.
   * GET /workshifts/YYYY-MM-DD
   *
   * @param date – התאריך הרצוי
   * @returns Observable עם רשימת המשמרות של אותו יום
   */
  getByDate(date: Date): Observable<WorkShiftResponse[]> {
    return this.http.get<WorkShiftResponse[]>(`${this.BASE}/${toLocalDateStr(date)}`);
  }

  loadByDate(date: Date): void {
    loadSignal(this.getByDate(date), this.shifts, this.loading);
  }

  /**
   * מוסיף משמרת עבודה חדשה.
   * POST /workshifts
   *
   * @param req – תאריך + שעת התחלה + שעת סיום
   * @returns Observable עם הודעת אישור
   */
  add(req: WorkShiftRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(this.BASE, req);
  }

  /**
   * מעדכן משמרת קיימת.
   * PUT /workshifts/:id
   *
   * @param id  – מזהה המשמרת
   * @param req – הנתונים המעודכנים
   * @returns Observable עם הודעת אישור
   */
  update(id: string, req: WorkShiftRequest): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.BASE}/${id}`, req);
  }

  /**
   * מוחק משמרת קיימת.
   * DELETE /workshifts/:id
   *
   * @param id – מזהה המשמרת
   * @returns Observable עם הודעת אישור
   */
  delete(id: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.BASE}/${id}`);
  }
}
