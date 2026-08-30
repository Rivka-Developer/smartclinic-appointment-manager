// src/app/features/auth/register/register.component.ts
// ─────────────────────────────────────────────────────────────────────────────
// דף הרשמה למערכת (Register Page).
//
// מאפשר ללקוחות חדשים ליצור חשבון עם:
//   - שם מלא, טלפון, מייל, סיסמה (מינימום 6 תווים)
//
// לאחר הרשמה מוצלחת: AuthService._persist() מנתב לדף הבית של הלקוח.
// בשגיאה: מציג הודעת snackbar (לרוב כשהמייל כבר קיים).
//
// קבצים:
//   register.component.html – תבנית הטופס
//   register.component.css  – עיצוב עמוד ההרשמה
// ─────────────────────────────────────────────────────────────────────────────

import { Component, inject, signal , ChangeDetectionStrategy} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { SnackService } from '../../../core/services/snack.service';
import { PolicyModalComponent } from '../../../shared/components/policy-modal/policy-modal.component';
import { GoogleSigninButtonComponent } from '../../../shared/components/google-signin-button/google-signin-button.component';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, PolicyModalComponent, GoogleSigninButtonComponent],
  templateUrl: './register.component.html',
  styleUrls: ['../auth.shared.css', './register.component.css']
})
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private snack = inject(SnackService);
  private router = inject(Router);

  loading       = signal(false);
  showPolicy    = signal(false);

  /**
   * הטופס הריאקטיבי עם ולידציות:
   * - fullName:    נדרש
   * - phoneNumber: נדרש (אין ולידציה על פורמט – הפשטה מכוונת)
   * - email:       נדרש + פורמט מייל תקין
   * - password:    נדרש + מינימום 6 תווים
   */
  form = this.fb.group({
    fullName:    ['', Validators.required],
    phoneNumber: ['', Validators.required],
    email:       ['', [Validators.required, Validators.email]],
    password:    ['', [Validators.required, Validators.minLength(6)]]
  });

  /**
   * נקרא בעת לחיצה על "הרשמה" (ngSubmit).
   *
   * 1. בודק תקינות הטופס
   * 2. מציג spinner
   * 3. שולח בקשת הרשמה
   * 4. בהצלחה: AuthService._persist() מנתב לדף הבית
   * 5. בשגיאה (לרוב מייל כבר קיים): מציג הודעה
   */
  submit(): void {
    if (this.form.invalid) return;
    this.loading.set(true);
    const v = this.form.value; // שולפים את כל ערכי הטופס
    this.auth.register({
      fullName: v.fullName!,
      email: v.email!,
      phoneNumber: v.phoneNumber!,
      password: v.password!
    }).subscribe({
      next: () => this.showPolicy.set(true),
      error: () => {
        this.snack.error('שגיאה בהרשמה. ייתכן שהאימייל כבר קיים');
        this.loading.set(false);
      }
    });
  }

  onPolicyAccepted(): void {
    this.router.navigate(['/client/home']);
  }

  /** נקרא לאחר שהמשתמש בחר חשבון Google; הניתוב נעשה אוטומטית ב-_persist() */
  onGoogleLogin(idToken: string): void {
    this.auth.loginWithGoogle(idToken).subscribe({
      error: () => this.snack.error('ההתחברות עם Google נכשלה')
    });
  }
}
