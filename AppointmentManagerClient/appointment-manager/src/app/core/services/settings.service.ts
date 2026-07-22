// src/app/core/services/settings.service.ts
// שירות הגדרות המערכת.
//
// אחראי על:
//   - שליפת הגדרות המערכת מהשרת (שעות פתיחה, משכי תורים, buffer)
//   - שמירתן ב-Signal לגישה מהירה מכל קומפוננטה
//   - עדכון ההגדרות (למנהלת)
//
// eveningStartMinutes – computed signal המחשב את שעת תחילת ערב בדקות.
// שאר הקומפוננטות קוראות load() ב-ngOnInit ואז קוראות eveningStartMinutes() ישירות.

import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { SystemSettingsDto } from '../models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class SettingsService {
  private http = inject(HttpClient);
  private readonly BASE = `${environment.apiUrl}/settings`;
  private _loading = false;

  /** ההגדרות הנוכחיות של המערכת – null עד לטעינה ראשונה */
  readonly settings = signal<SystemSettingsDto | null>(null);

  /**
   * שעת תחילת ערב בדקות מחצות (ברירת מחדל 960 = 16:00).
   * מחושב אוטומטית מ-settings; לא צריך לחשב בכל קומפוננטה בנפרד.
   */
  readonly eveningStartMinutes = computed(() => {
    const s = this.settings();
    if (!s) return 960;
    const parts = (s.eveningStartTime ?? '16:00:00').split(':').map(Number);
    return parts[0] * 60 + (parts[1] || 0);
  });

  /** שולף את ההגדרות מהשרת ומחזיר Observable */
  get(): Observable<SystemSettingsDto> {
    return this.http.get<SystemSettingsDto>(this.BASE);
  }

  /**
   * טוען הגדרות ושומר ב-settings Signal.
   * קריאה כפולה מ-2 קומפוננטות מטופלת – הבקשה נשלחת רק פעם אחת.
   */
  load(): void {
    if (this.settings() || this._loading) return;
    this._loading = true;
    this.get().subscribe({
      next: s => { this.settings.set(s); this._loading = false; },
      error: () => { this._loading = false; }
    });
  }

  /**
   * מעדכן את הגדרות המערכת (פעולה זמינה למנהלת בלבד).
   * PUT /settings
   */
  update(dto: SystemSettingsDto): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(this.BASE, dto);
  }
}
