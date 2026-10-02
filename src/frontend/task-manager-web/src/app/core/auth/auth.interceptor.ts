import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { API_CONFIG, ApiConfig } from '../api-config';
import { AuthService } from './auth.service';

export function isProtectedApi(url: string, config: ApiConfig): boolean {
  try {
    const target = new URL(url, document.baseURI);
    const auth = new URL(`${config.authApiBaseUrl}/api/auth/me`);
    const tasks = new URL(`${config.taskApiBaseUrl}/api/tasks`);
    return (
      (target.origin === auth.origin && target.pathname === auth.pathname) ||
      (target.origin === tasks.origin &&
        (target.pathname === tasks.pathname || target.pathname.startsWith(`${tasks.pathname}/`)))
    );
  } catch {
    return false;
  }
}

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const token = isProtectedApi(request.url, inject(API_CONFIG)) ? auth.getToken() : null;
  return next(
    token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request,
  ).pipe(
    catchError((error) => {
      if (token && error instanceof HttpErrorResponse && error.status === 401)
        auth.invalidate(token);
      return throwError(() => error);
    }),
  );
};
