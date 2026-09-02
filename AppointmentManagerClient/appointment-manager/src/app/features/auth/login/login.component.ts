// src/app/features/auth/login/login.component.ts
// ─────────────────────────────────────────────────────────────────────────────
// דף כניסה למערכת (Login Page).
//
// מציג טופס עם שדות אימייל וסיסמה.
// בעת שליחה תקינה: שולח בקשה ל-AuthService.login() ומחכה לתגובה.
// ניתוב לדף הבית נעשה אוטומטית ב-AuthService._persist() לאחר כניסה מוצלחת.
// בשגיאה: מציג הודעת snackbar אדומה.
//
// קבצים:
//   login.component.html – תבנית הטופס
//   login.component.css  – עיצוב עמוד הכניסה
//
// ה-guestGuard על הנתיב מבטיח שמשתמשים מחוברים לא יגיעו לדף זה.
// ─────────────────────────────────────────────────────────────────────────────

import { Component, inject, signal , ChangeDetectionStrategy} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { SnackService } from '../../../core/services/snack.service';
import { GoogleSigninButtonComponent } from '../../../shared/components/google-signin-button/google-signin-button.component';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { SpinnerComponent } from '../../../shared/components/spinner/spinner.component';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-login',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    GoogleSigninButtonComponent,
    IconComponent,
    SpinnerComponent
  ],
  templateUrl: './login.component.html',
  styleUrls: ['../auth.shared.css', './login.component.css']
})
export class LoginComponent {
  private fb = inject(FormBuilder);   // בונה FormGroup בצורה נוחה
  private auth = inject(AuthService);  // לשליחת בקשת כניסה
  private snack = inject(SnackService); // להצגת הודעות שגיאה

  /** האם נשלחת בקשת HTTP ועדיין ממתינים לתשובה? */
  loading = signal(false);

  /** האם להציג את הסיסמה כטקסט גלוי (טוגל עין) */
  showPassword = signal(false);

  /**
   * הטופס הריאקטיבי עם ולידציה:
   * - email:    נדרש + פורמט מייל תקין
   * - password: נדרש (אין ולידציה על אורך בכניסה)
   */
  form = this.fb.group({
    email:    ['', [Validators.required, Validators.email]],
    password: ['', Validators.required]
  });

  /**
   * נקרא בעת לחיצה על "כניסה" (ngSubmit).
   *
   * 1. בודק שהטופס תקין (כפול – גם הכפתור מושבת, אבל הגנה נוספת)
   * 2. מציג spinner
   * 3. שולח בקשת כניסה
   * 4. בהצלחה: AuthService._persist() מנתב אוטומטית
   * 5. בשגיאה: מציג הודעה ומכבה spinner
   */
  submit(): void {
    if (this.form.invalid) return; // הגנה כפולה – לא אמור לקרות בגלל [disabled]
    this.loading.set(true);
    const { email, password } = this.form.value;
    this.auth.login({ email: email!, password: password! }).subscribe({
      next: () => { /* הניתוב נעשה אוטומטית ב-_persist() ב-AuthService */ },
      error: () => {
        this.snack.error('אימייל או סיסמה שגויים');
        this.loading.set(false); // מכבים spinner כדי לאפשר ניסיון נוסף
      }
    });
  }

  /** מחליף בין הצגת הסיסמה כטקסט גלוי לבין הסתרתה */
  togglePasswordVisibility(): void {
    this.showPassword.update(v => !v);
  }

  /** נקרא לאחר שהמשתמש בחר חשבון Google; הניתוב נעשה אוטומטית ב-_persist() */
  onGoogleLogin(idToken: string): void {
    this.loading.set(true); // מציגים ספינר עד שהשרת מאמת מול גוגל ומחזיר תשובה
    this.auth.loginWithGoogle(idToken).subscribe({
      // 404 = אין חשבון עם המייל הזה (כניסה עם Google לא יוצרת חשבון חדש) - מציגים את הודעת השרת
      error: (err) => {
        this.snack.error(err?.error?.detail ?? 'ההתחברות עם Google נכשלה');
        this.loading.set(false);
      }
    });
  }
}
