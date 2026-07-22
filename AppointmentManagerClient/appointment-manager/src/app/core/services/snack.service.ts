// src/app/core/services/snack.service.ts
// ─────────────────────────────────────────────────────────────────────────────
// שירות הודעות קופצות (Snackbar / Toast).
//
// מאפשר להציג הודעות זמניות למשתמש בתחתית המסך:
//   - הצלחה (ירוק)  – למשל "התור נקבע בהצלחה"
//   - שגיאה (אדום)   – למשל "אימייל או סיסמה שגויים"
//   - מידע (כחול)    – למשל "2 משמרות נוספו, 1 נכשלה"
//
// הקומפוננטה SnackbarComponent מאזינה ל-Signal זה ומציגה את ההודעה.
// ההודעה נעלמת אוטומטית אחרי 4 שניות.
// ─────────────────────────────────────────────────────────────────────────────

import { Injectable, signal } from '@angular/core';

/** מבנה ההודעה הקופצת */
export interface SnackMessage {
  text: string;                        // טקסט ההודעה לתצוגה
  type: 'success' | 'error' | 'info'; // סוג ההודעה (קובע את הצבע)
}

@Injectable({ providedIn: 'root' })
export class SnackService {
  /**
   * ה-Signal של ההודעה הנוכחית.
   * null = אין הודעה מוצגת כרגע.
   * SnackbarComponent מאזין לשינויים ומציג/מסתיר את ה-snackbar.
   */
  readonly message = signal<SnackMessage | null>(null);

  /**
   * מציג הודעה קופצת למשך 4 שניות.
   *
   * @param text – טקסט ההודעה
   * @param type – סוג ('success' | 'error' | 'info'), ברירת מחדל: 'info'
   */
  show(text: string, type: SnackMessage['type'] = 'info'): void {
    this.message.set({ text, type }); // מציגים את ההודעה
    setTimeout(() => this.message.set(null), 4000); // מסתירים אחרי 4 שניות
  }

  /** קיצור דרך להצגת הודעת הצלחה (ירוקה) */
  success(text: string) { this.show(text, 'success'); }

  /** קיצור דרך להצגת הודעת שגיאה (אדומה) */
  error(text: string)   { this.show(text, 'error'); }

  /** קיצור דרך להצגת הודעת מידע (כחולה) */
  info(text: string)    { this.show(text, 'info'); }
}
