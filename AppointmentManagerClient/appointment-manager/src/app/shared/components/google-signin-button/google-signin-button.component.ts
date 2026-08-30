// src/app/shared/components/google-signin-button/google-signin-button.component.ts
// ─────────────────────────────────────────────────────────────────────────────
// כפתור "התחברות עם Google" (Google Identity Services).
//
// טוען את ה-Client ID מ-environment, מאתחל את google.accounts.id
// ומצייר את הכפתור הרשמי של Google בתוך div. בעת בחירת חשבון,
// גוגל מחזירה ID Token חתום דרך callback — הרכיב פולט אותו החוצה
// כדי שהקומפוננטה ההורה (login/register) תשלח אותו לשרת לאימות.
//
// הסקריפט https://accounts.google.com/gsi/client נטען גלובלית ב-index.html.
// ─────────────────────────────────────────────────────────────────────────────

import { AfterViewInit, Component, ElementRef, EventEmitter, Output, ViewChild } from '@angular/core';
import { environment } from '../../../../environments/environment';

declare global {
  interface Window {
    google?: any;
  }
}

@Component({
  selector: 'app-google-signin-button',
  standalone: true,
  templateUrl: './google-signin-button.component.html',
  styleUrls: ['./google-signin-button.component.css']
})
export class GoogleSigninButtonComponent implements AfterViewInit {
  @ViewChild('buttonContainer', { static: true }) buttonContainer!: ElementRef<HTMLDivElement>;

  /** פולט את ה-ID Token שהתקבל מ-Google לאחר בחירת חשבון */
  @Output() googleToken = new EventEmitter<string>();

  ngAfterViewInit(): void {
    if (!window.google) return; // הסקריפט של Google עדיין לא נטען (רשת איטית וכו')

    window.google.accounts.id.initialize({
      client_id: environment.googleClientId,
      callback: (response: { credential: string }) => this.googleToken.emit(response.credential)
    });

    window.google.accounts.id.renderButton(this.buttonContainer.nativeElement, {
      type: 'standard',
      theme: 'outline',
      size: 'large',
      width: 320,
      locale: 'he'
    });
  }
}
