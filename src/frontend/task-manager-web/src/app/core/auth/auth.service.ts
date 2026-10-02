import { HttpClient } from '@angular/common/http';
import { computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom, tap, timeout } from 'rxjs';
import { API_CONFIG } from '../api-config';
import { AuthInput, AuthResult, CurrentUser } from './auth.models';

export const SESSION_KEY = 'task-manager.access-token';

// UX expiry check only. The backend verifies signatures and authorizes requests.
export function tokenExpiry(token: string): number | null {
  try {
    const parts = token.split('.');
    if (parts.length !== 3 || parts.some((part) => !/^[A-Za-z0-9_-]+$/.test(part))) return null;
    const payload = JSON.parse(atob(parts[1].replace(/-/g, '+').replace(/_/g, '/')));
    return typeof payload.exp === 'number' &&
      Number.isFinite(payload.exp) &&
      payload.exp * 1000 > Date.now()
      ? payload.exp * 1000
      : null;
  } catch {
    return null;
  }
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly config = inject(API_CONFIG);
  private readonly user = signal<CurrentUser | null>(null);
  private readonly token = signal<string | null>(null);
  private readonly ready = signal(false);
  private restoration?: Promise<void>;
  private expiryTimer?: ReturnType<typeof setTimeout>;
  private revision = 0;
  readonly currentUser = this.user.asReadonly();
  readonly initialized = this.ready.asReadonly();
  readonly isAuthenticated = computed(() => this.user() !== null && this.token() !== null);

  constructor() {
    inject(DestroyRef).onDestroy(() => clearTimeout(this.expiryTimer));
  }

  initialize(): Promise<void> {
    return (this.restoration ??= this.restore());
  }
  login(input: AuthInput) {
    return this.authenticate('login', input);
  }
  register(input: AuthInput) {
    return this.authenticate('register', input);
  }

  private authenticate(action: 'login' | 'register', input: AuthInput) {
    const revision = this.revision;
    return this.http
      .post<AuthResult>(`${this.config.authApiBaseUrl}/api/auth/${action}`, input)
      .pipe(
        timeout(10000),
        tap((result) => {
          if (revision !== this.revision) throw new Error('Session changed.');
          const expiry = tokenExpiry(result.accessToken);
          if (!expiry || !this.validUser(result))
            throw new Error('Invalid authentication response.');
          try {
            sessionStorage.setItem(SESSION_KEY, result.accessToken);
          } catch {
            throw new Error('Session storage is unavailable.');
          }
          this.establish(result.accessToken, result, expiry);
        }),
      );
  }

  getToken(): string | null {
    const token = this.token();
    if (token && !tokenExpiry(token)) {
      this.invalidate(token);
      return null;
    }
    return token;
  }

  logout(): Promise<boolean> {
    this.clear();
    return this.router.navigate(['/login']);
  }

  invalidate(requestToken: string): void {
    // Late responses from older requests must not destroy a newer session.
    if (this.token() !== requestToken) return;
    this.clear();
    void this.router.navigate(['/login'], { queryParams: { session: 'expired' } });
  }

  private async restore(): Promise<void> {
    const revision = this.revision;
    try {
      const stored = sessionStorage.getItem(SESSION_KEY);
      const expiry = stored ? tokenExpiry(stored) : null;
      if (!stored || !expiry) {
        this.clear();
        return;
      }
      this.token.set(stored);
      const user = await firstValueFrom(
        this.http
          .get<CurrentUser>(`${this.config.authApiBaseUrl}/api/auth/me`)
          .pipe(timeout(10000)),
      );
      if (revision !== this.revision) return;
      if (!this.validUser(user) || !tokenExpiry(stored)) {
        this.clear();
        return;
      }
      this.establish(stored, user, expiry);
    } catch {
      if (revision === this.revision) this.clear();
    } finally {
      this.ready.set(true);
    }
  }

  private validUser(user: CurrentUser): boolean {
    return (
      Number.isSafeInteger(user?.userId) &&
      user.userId > 0 &&
      typeof user.username === 'string' &&
      user.username.trim().length > 0
    );
  }

  private establish(token: string, user: CurrentUser, expiry: number): void {
    clearTimeout(this.expiryTimer);
    this.token.set(token);
    this.user.set({ userId: user.userId, username: user.username });
    this.ready.set(true);
    this.expiryTimer = setTimeout(
      () => this.invalidate(token),
      Math.min(expiry - Date.now(), 2147483647),
    );
  }

  private clear(): void {
    this.revision++;
    clearTimeout(this.expiryTimer);
    this.token.set(null);
    this.user.set(null);
    try {
      sessionStorage.removeItem(SESSION_KEY);
    } catch {
      /* Browser storage may be blocked. */
    }
  }
}
