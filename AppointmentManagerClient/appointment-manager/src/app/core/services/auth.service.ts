import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { AuthResponse, GoogleLoginRequest, LoginRequest, RegisterRequest } from '../models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http   = inject(HttpClient);
  private router = inject(Router);

  private readonly BASE = `${environment.apiUrl}/auth`;

  // ─── מצב תגובתי ────────────────────────────────────────────────────────────
  // ה-JWT נמצא ב-HttpOnly Cookie — JavaScript לא ניגש אליו ישירות.
  // שומרים רק את פרטי המשתמש (שם + תפקיד) ב-localStorage לשחזור session.
  private _user = signal<{ fullName: string; role: string } | null>(
    this._loadUserFromStorage()
  );

  readonly user      = this._user.asReadonly();
  readonly isLoggedIn = computed(() => !!this._user());
  readonly isAdmin    = computed(() => this._user()?.role === 'Admin');

  // ─── קריאות API ────────────────────────────────────────────────────────────

  login(body: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.BASE}/login`, body).pipe(
      tap(res => this._persist(res))
    );
  }

  register(body: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.BASE}/register`, body).pipe(
      tap(res => this._persistUser(res))
    );
  }

  loginWithGoogle(idToken: string): Observable<AuthResponse> {
    const body: GoogleLoginRequest = { idToken };
    return this.http.post<AuthResponse>(`${this.BASE}/google`, body).pipe(
      tap(res => this._persist(res))
    );
  }

  logout(): void {
    localStorage.removeItem('user');
    this._user.set(null);
    // מנקה את ה-Cookie בשרת; withCredentials נשלח אוטומטית ע"י ה-interceptor
    this.http.post(`${this.BASE}/logout`, {}).subscribe({ error: () => {} });
    this.router.navigate(['/login']);
  }

  // ─── פונקציות עזר ──────────────────────────────────────────────────────────

  private _persist(res: AuthResponse): void {
    const userObj = { fullName: res.fullName, role: res.role };
    localStorage.setItem('user', JSON.stringify(userObj));
    this._user.set(userObj);
    this.router.navigate(userObj.role === 'Admin' ? ['/admin/home'] : ['/client/home']);
  }

  // שמירת המשתמש ללא ניתוב — משמש לאחר הרשמה כדי לאפשר לקומפוננטה לנתב לדף המדיניות
  _persistUser(res: AuthResponse): void {
    const userObj = { fullName: res.fullName, role: res.role };
    localStorage.setItem('user', JSON.stringify(userObj));
    this._user.set(userObj);
  }

  private _loadUserFromStorage(): { fullName: string; role: string } | null {
    try {
      const raw = localStorage.getItem('user');
      return raw ? JSON.parse(raw) : null;
    } catch {
      return null;
    }
  }
}
