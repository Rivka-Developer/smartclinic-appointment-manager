import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, timeout } from 'rxjs/operators';
import { throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

const REQUEST_TIMEOUT_MS = 15_000;

export const jwtInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  // הוסף withCredentials כדי שהדפדפן ישלח את ה-HttpOnly Cookie אוטומטית
  req = req.clone({ withCredentials: true });

  const timeoutMs = REQUEST_TIMEOUT_MS;

  return next(req).pipe(
    timeout(timeoutMs),
    catchError((err: HttpErrorResponse) => {
      if (err.status === 401) authService.logout();
      return throwError(() => err);
    })
  );
};
