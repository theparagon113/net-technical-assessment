import {
  HttpClient,
  HttpInterceptorFn,
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { firstValueFrom } from 'rxjs';
import { describe, expect, it } from 'vitest';
import { routes } from '../../app.routes';
import { apiConfig } from '../api-config';
import { authInterceptor } from './auth.interceptor';
import { CurrentUser } from './auth.models';
import { AuthService, SESSION_KEY } from './auth.service';

const live =
  (globalThis as typeof globalThis & { process?: { env: Record<string, string> } }).process?.env[
    'M5_LIVE'
  ] === '1';

// Opt-in: real hosts, disposable shared database, no task UI or manually inserted header.
describe.skipIf(!live)('Live two-host Angular authentication flow', () => {
  it('registers, logs in, restores /me, authenticates Task.Api, then guards logout', async () => {
    const observations: { url: string; bearer: boolean }[] = [];
    const observe: HttpInterceptorFn = (request, next) => {
      observations.push({ url: request.url, bearer: request.headers.has('Authorization') });
      return next(request);
    };
    const configure = () =>
      TestBed.configureTestingModule({
        providers: [
          provideRouter(routes),
          provideHttpClient(withInterceptors([authInterceptor, observe])),
        ],
      });
    sessionStorage.clear();
    configure();
    let auth = TestBed.inject(AuthService);
    await auth.initialize();
    const input = { username: `m5-${Date.now()}`, password: 'Assessment123!' };
    const registered = await firstValueFrom(auth.register(input));
    expect(registered.username).toBe(input.username);
    expect(sessionStorage.getItem(SESSION_KEY)).toBe(registered.accessToken);
    expect(localStorage.getItem(SESSION_KEY)).toBeNull();
    await auth.logout();
    await firstValueFrom(auth.login(input));
    const token = sessionStorage.getItem(SESSION_KEY);
    const me = await firstValueFrom(
      TestBed.inject(HttpClient).get<CurrentUser>(`${apiConfig.authApiBaseUrl}/api/auth/me`),
    );
    expect(me.username).toBe(input.username);
    const tasks = await firstValueFrom(
      TestBed.inject(HttpClient).get<unknown[]>(`${apiConfig.taskApiBaseUrl}/api/tasks`),
    );
    expect(tasks).toEqual([]);
    // A new service instance represents same-tab application restart with only stored JWT.
    TestBed.resetTestingModule();
    configure();
    auth = TestBed.inject(AuthService);
    await auth.initialize();
    expect(auth.currentUser()?.username).toBe(input.username);
    expect(auth.getToken()).toBe(token);
    const harness = await RouterTestingHarness.create('/tasks');
    expect(TestBed.inject(Router).url).toBe('/tasks');
    await auth.logout();
    await harness.navigateByUrl('/tasks');
    expect(TestBed.inject(Router).url).toBe('/login');
    expect(sessionStorage.getItem(SESSION_KEY)).toBeNull();
    expect(
      observations
        .filter((x) => x.url.endsWith('/login') || x.url.endsWith('/register'))
        .every((x) => !x.bearer),
    ).toBe(true);
    expect(
      observations
        .filter((x) => x.url.endsWith('/me') || x.url.endsWith('/tasks'))
        .every((x) => x.bearer),
    ).toBe(true);
  });
});
