// src/app/core/guards/auth.guard.ts
// ─────────────────────────────────────────────────────────────────────────────
// קובץ זה מכיל שלוש פונקציות שמירה (Guards) על הניתוב.
// Guard הוא פונקציה שבודקת תנאי לפני כניסה לנתיב –
// אם התנאי מתקיים, הגלישה ממשיכה; אחרת, מועברים לדף אחר.
// ─────────────────────────────────────────────────────────────────────────────

import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * authGuard – שומר על נתיבים שדורשים כניסה למערכת.
 *
 * מונע כניסה לדפים פנימיים (כגון לוח שנה, תורים) למשתמשים שלא מחוברים.
 * שימוש: canActivate: [authGuard] בהגדרת הנתיב.
 *
 * @returns true אם המשתמש מחובר; מנווט ל-/login ומחזיר false אחרת.
 */
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);   // שירות האימות – מכיל מצב התחברות
  const router = inject(Router);       // נתב Angular לניווט תוכניתי

  if (auth.isLoggedIn()) return true;  // המשתמש מחובר → מאשרים כניסה

  // המשתמש לא מחובר → מנתבים לדף הכניסה ומסרבים לכניסה לנתיב
  router.navigate(['/login']);
  return false;
};

/**
 * adminGuard – שומר על נתיבים שדורשים הרשאת מנהלת.
 *
 * מונע כניסה ללוח הניהול מלקוחות רגילים.
 * יש להשתמש בו בנוסף ל-authGuard (כלומר: [authGuard, adminGuard]).
 *
 * @returns true אם המשתמש הוא מנהל; מנווט ל-/client ומחזיר false אחרת.
 */
export const adminGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAdmin()) return true;    // המשתמש הוא מנהלת → מאשרים כניסה

  // המשתמש הוא לקוח רגיל → מנתבים לדף הבית של הלקוח
  router.navigate(['/client']);
  return false;
};

/**
 * guestGuard – שומר על נתיבים שרק משתמשים לא מחוברים יכולים לגשת אליהם.
 *
 * מניח שמשתמש מחובר לא צריך לראות את דפי הכניסה/הרשמה.
 * אם המשתמש כבר מחובר, מנתבים אותו ישירות לדף הבית המתאים.
 *
 * @returns true אם המשתמש לא מחובר; מנווט לדף הבית המתאים ומחזיר false אחרת.
 */
export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isLoggedIn()) return true; // לא מחובר → מאשרים כניסה לדף הכניסה/הרשמה

  // המשתמש כבר מחובר → מנתבים לדף הבית שלו לפי תפקידו
  router.navigate(auth.isAdmin() ? ['/admin/home'] : ['/client/home']);
  return false;
};
