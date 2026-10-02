import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { routes } from '../../app.routes';
import { apiConfig } from '../api-config';
import { authInterceptor } from './auth.interceptor';
import { AuthService, SESSION_KEY } from './auth.service';

export function testToken(exp = Math.floor(Date.now() / 1000) + 900): string {
  return `${btoa('{"alg":"HS256"}')}.${btoa(JSON.stringify({ exp })).replace(/=/g, '')}.signature`;
}

describe('Authentication session and HTTP boundary', () => {
  let auth: AuthService;
  let http: HttpTestingController;
  const input = { username: 'demo', password: 'Demo123!' };
  const user = { userId: 1, username: 'demo' };
  beforeEach(() => {
    sessionStorage.clear();
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter(routes),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    auth = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => {
    http.verify();
    auth.logout();
    sessionStorage.clear();
    vi.useRealTimers();
  });

  async function login(token = testToken()) {
    const result = firstValueFrom(auth.login(input));
    const request = http.expectOne(`${apiConfig.authApiBaseUrl}/api/auth/login`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(input);
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({
      ...user,
      accessToken: token,
      expiresAt: new Date(Date.now() + 900000).toISOString(),
    });
    await result;
    return token;
  }

  it('login stores only the JWT in sessionStorage and exposes safe identity', async () => {
    const token = await login();
    expect(sessionStorage.getItem(SESSION_KEY)).toBe(token);
    expect(sessionStorage.length).toBe(1);
    expect(localStorage.length).toBe(0);
    expect(auth.currentUser()).toEqual(user);
    expect(auth.isAuthenticated()).toBe(true);
  });
  it('does not establish a session when browser storage is blocked', async () => {
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new Error('blocked');
    });
    const result = firstValueFrom(auth.login(input));
    http
      .expectOne(`${apiConfig.authApiBaseUrl}/api/auth/login`)
      .flush({ ...user, accessToken: testToken(), expiresAt: new Date().toISOString() });
    await expect(result).rejects.toThrow('Session storage is unavailable.');
    expect(auth.isAuthenticated()).toBe(false);
    vi.restoreAllMocks();
  });
  it('rejects malformed safe identity during restoration', async () => {
    sessionStorage.setItem(SESSION_KEY, testToken());
    const restore = auth.initialize();
    http
      .expectOne(`${apiConfig.authApiBaseUrl}/api/auth/me`)
      .flush({ userId: 0, username: 'demo' });
    await restore;
    expect(auth.isAuthenticated()).toBe(false);
    expect(sessionStorage.getItem(SESSION_KEY)).toBeNull();
  });
  it('registration uses the real Auth.Api contract and establishes its returned session', async () => {
    const result = firstValueFrom(auth.register(input));
    const request = http.expectOne(`${apiConfig.authApiBaseUrl}/api/auth/register`);
    expect(request.request.headers.has('Authorization')).toBe(false);
    expect(request.request.body).toEqual(input);
    request.flush(
      { ...user, accessToken: testToken(), expiresAt: new Date().toISOString() },
      { status: 201, statusText: 'Created' },
    );
    await result;
    expect(auth.isAuthenticated()).toBe(true);
  });
  it('restores a same-tab token only after authoritative /me succeeds', async () => {
    const token = testToken();
    sessionStorage.setItem(SESSION_KEY, token);
    const restore = auth.initialize();
    expect(auth.isAuthenticated()).toBe(false);
    const request = http.expectOne(`${apiConfig.authApiBaseUrl}/api/auth/me`);
    expect(request.request.headers.get('Authorization')).toBe(`Bearer ${token}`);
    request.flush(user);
    await restore;
    expect(auth.currentUser()).toEqual(user);
    expect(auth.initialized()).toBe(true);
    await auth.initialize();
    http.expectNone(`${apiConfig.authApiBaseUrl}/api/auth/me`);
  });
  it.each(['bad', testToken(1), 'a.eyJleHAiOiJ3cm9uZyJ9.c'])(
    'rejects unusable stored state (%s)',
    async (token) => {
      sessionStorage.setItem(SESSION_KEY, token);
      await auth.initialize();
      expect(auth.isAuthenticated()).toBe(false);
      expect(sessionStorage.getItem(SESSION_KEY)).toBeNull();
      http.expectNone(`${apiConfig.authApiBaseUrl}/api/auth/me`);
    },
  );
  it.each([401, 0, 503])('clears failed restoration with status %s', async (status) => {
    sessionStorage.setItem(SESSION_KEY, testToken());
    const restore = auth.initialize();
    http
      .expectOne(`${apiConfig.authApiBaseUrl}/api/auth/me`)
      .flush(null, { status, statusText: 'Failure' });
    await restore;
    expect(auth.isAuthenticated()).toBe(false);
    expect(auth.getToken()).toBeNull();
    expect(sessionStorage.getItem(SESSION_KEY)).toBeNull();
  });
  it('logout during restoration cannot resurrect a session', async () => {
    sessionStorage.setItem(SESSION_KEY, testToken());
    const restore = auth.initialize();
    const request = http.expectOne(`${apiConfig.authApiBaseUrl}/api/auth/me`);
    auth.logout();
    request.flush(user);
    await restore;
    expect(auth.isAuthenticated()).toBe(false);
    expect(auth.getToken()).toBeNull();
  });
  it('expires a running session and removes storage', async () => {
    vi.useFakeTimers();
    await login(testToken(Math.floor(Date.now() / 1000) + 2));
    vi.advanceTimersByTime(2100);
    expect(auth.isAuthenticated()).toBe(false);
    expect(sessionStorage.getItem(SESSION_KEY)).toBeNull();
  });
  it('forwards the session to both protected APIs and never to unrelated origins or paths', async () => {
    const token = await login();
    const client = TestBed.inject(HttpClient);
    const urls = [
      [`${apiConfig.authApiBaseUrl}/api/auth/me`, true],
      [`${apiConfig.taskApiBaseUrl}/api/tasks`, true],
      [`${apiConfig.taskApiBaseUrl}/api/tasks/42?view=1`, true],
      ['https://unrelated.example/api/tasks', false],
      ['http://localhost:5149.evil.example/api/tasks', false],
      [`${apiConfig.taskApiBaseUrl}/api/tasks-other`, false],
      [`${apiConfig.authApiBaseUrl}/api/auth/public`, false],
      [`${apiConfig.authApiBaseUrl}/api/auth/login`, false],
      [`${apiConfig.authApiBaseUrl}/api/auth/register`, false],
      [`${apiConfig.taskApiBaseUrl}/assets/image.svg`, false],
    ] as const;
    for (const [url, attach] of urls) {
      const result = firstValueFrom(client.get(url));
      const request = http.expectOne(url);
      expect(request.request.headers.get('Authorization')).toBe(attach ? `Bearer ${token}` : null);
      request.flush({});
      await result;
    }
  });
  it('login 401 stays a feature error without global session navigation', async () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate');
    const result = firstValueFrom(auth.login(input));
    http
      .expectOne(`${apiConfig.authApiBaseUrl}/api/auth/login`)
      .flush({}, { status: 401, statusText: 'Unauthorized' });
    await expect(result).rejects.toMatchObject({ status: 401 });
    expect(navigate).not.toHaveBeenCalled();
  });
  it('two protected 401s invalidate once and preserve the errors', async () => {
    await login();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate');
    const client = TestBed.inject(HttpClient);
    const a = firstValueFrom(client.get(`${apiConfig.taskApiBaseUrl}/api/tasks`));
    const b = firstValueFrom(client.get(`${apiConfig.authApiBaseUrl}/api/auth/me`));
    for (const url of [
      `${apiConfig.taskApiBaseUrl}/api/tasks`,
      `${apiConfig.authApiBaseUrl}/api/auth/me`,
    ])
      http.expectOne(url).flush({}, { status: 401, statusText: 'Unauthorized' });
    await expect(a).rejects.toMatchObject({ status: 401 });
    await expect(b).rejects.toMatchObject({ status: 401 });
    expect(auth.isAuthenticated()).toBe(false);
    expect(navigate).toHaveBeenCalledTimes(1);
  });
  it('late 401 for an older token cannot invalidate a new login', async () => {
    await login();
    const result = firstValueFrom(
      TestBed.inject(HttpClient).get(`${apiConfig.taskApiBaseUrl}/api/tasks`),
    );
    const oldRequest = http.expectOne(`${apiConfig.taskApiBaseUrl}/api/tasks`);
    const next = await login(testToken(Math.floor(Date.now() / 1000) + 1000));
    oldRequest.flush({}, { status: 401, statusText: 'Unauthorized' });
    await expect(result).rejects.toMatchObject({ status: 401 });
    expect(auth.getToken()).toBe(next);
  });
  it('anonymous /tasks redirects to /login; logout blocks later navigation', async () => {
    const harness = await RouterTestingHarness.create('/tasks');
    expect(TestBed.inject(Router).url).toBe('/login');
    await login();
    await harness.navigateByUrl('/tasks');
    expect(TestBed.inject(Router).url).toBe('/tasks');
    await harness.navigateByUrl('/register');
    expect(TestBed.inject(Router).url).toBe('/tasks');
    await auth.logout();
    await harness.navigateByUrl('/tasks');
    expect(TestBed.inject(Router).url).toBe('/login');
    expect(sessionStorage.getItem(SESSION_KEY)).toBeNull();
  });
  it('guard waits for initial restoration and rejects expired state', async () => {
    sessionStorage.setItem(SESSION_KEY, testToken());
    const harnessPromise = RouterTestingHarness.create('/tasks');
    await vi.waitFor(() => http.expectOne(`${apiConfig.authApiBaseUrl}/api/auth/me`).flush(user));
    const harness = await harnessPromise;
    expect(TestBed.inject(Router).url).toBe('/tasks');
    await auth.logout();
    await harness.navigateByUrl('/tasks');
    expect(TestBed.inject(Router).url).toBe('/login');
  });
  it('expired storage cannot pass the route guard', async () => {
    sessionStorage.setItem(SESSION_KEY, testToken(1));
    await RouterTestingHarness.create('/tasks');
    expect(TestBed.inject(Router).url).toBe('/login');
  });
});
