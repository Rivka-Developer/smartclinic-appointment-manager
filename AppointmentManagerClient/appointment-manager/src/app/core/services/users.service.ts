// src/app/core/services/users.service.ts
// ─────────────────────────────────────────────────────────────────────────────
// שירות ניהול משתמשים (לקוחות).
//
// אחראי על:
//   - שליפת רשימת כל הלקוחות הרשומים (למנהלת)
//   - שמירת הרשימה ב-Signal לגישה מהירה
//   - שליפת היסטוריית תורים של לקוח ספציפי
//
// משמש בעיקר ב-ClientsComponent (דף ניהול לקוחות של המנהלת).
// ─────────────────────────────────────────────────────────────────────────────

import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { UserResponse, UserHistoryResponse } from '../models';
import { environment } from '../../../environments/environment';
import { loadSignal } from '../utils/signal.utils';

/**
 * מבנה תשובה מדופדפת מהשרת.
 * השרת מחזיר רשימות בפורמט זה עם מידע על דפדוף.
 * T = סוג הפריטים (לדוגמה UserResponse)
 */
interface PagedResult<T> {
  items: T[];         // מערך הפריטים בדף הנוכחי
  totalCount: number; // סה"כ פריטים בכל הדפים
  pageNumber: number; // מספר הדף הנוכחי (מ-1)
  pageSize: number;   // גודל דף (כמה פריטים בכל דף)
}

@Injectable({ providedIn: 'root' })
export class UsersService {
  private http = inject(HttpClient);

  /** כתובת הבסיס של נקודות הקצה למשתמשים */
  private readonly BASE = `${environment.apiUrl}/users`;

  /** רשימת הלקוחות הרשומים – מתעדכן בעת קריאה ל-loadClients() */
  readonly clients = signal<UserResponse[]>([]);

  /** האם מתבצעת כרגע בקשת HTTP? – לשליטה בתצוגת spinner */
  readonly loading = signal(false);

  /**
   * מקבל את רשימת כל הלקוחות הרשומים.
   * GET /users/clients
   *
   * השרת מחזיר PagedResult, אנחנו שולפים רק את items (רשימת הלקוחות עצמה).
   *
   * @returns Observable עם מערך פרטי הלקוחות
   */
  getAllClients(): Observable<UserResponse[]> {
    return this.http.get<PagedResult<UserResponse>>(`${this.BASE}/clients`).pipe(
      map(r => r.items) // פועל (operator) שמחלץ רק את רשימת הפריטים מהעטיפה
    );
  }

  /**
   * טוען את רשימת הלקוחות ושומר ב-Signal clients.
   * מנהל loading state לתצוגת spinner.
   * נקרא מ-ClientsComponent ב-ngOnInit.
   */
  loadClients(): void {
    loadSignal(this.getAllClients(), this.clients, this.loading);
  }

  /**
   * מקבל את היסטוריית התורים המלאה של לקוח ספציפי.
   * GET /users/:id/history
   *
   * @param id – מזהה הלקוח (GUID)
   * @returns Observable עם פרטי הלקוח וכל תוריו
   */
  getClientHistory(id: string): Observable<UserHistoryResponse> {
    return this.http.get<UserHistoryResponse>(`${this.BASE}/${id}/history`);
  }

  /**
   * מוחק לקוח ואת כל התורים שלו לצמיתות.
   * DELETE /users/:id
   *
   * @param id – מזהה הלקוח (GUID)
   */
  deleteUser(id: string): Observable<void> {
    return this.http.delete<void>(`${this.BASE}/${id}`);
  }
}
