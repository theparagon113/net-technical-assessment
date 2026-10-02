import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { routes } from '../app.routes';
import { apiConfig } from '../core/api-config';
import { AuthService } from '../core/auth/auth.service';
import { AuthPage } from './auth-page';

for (const registering of [false, true]) {
  describe(registering ? 'Registration form' : 'Login form', () => {
    let http: HttpTestingController;
    let page: AuthPage;
    let harness: RouterTestingHarness;
    const endpoint = `${apiConfig.authApiBaseUrl}/api/auth/${registering ? 'register' : 'login'}`;
    beforeEach(async () => {
      sessionStorage.clear();
      TestBed.configureTestingModule({
        providers: [provideRouter(routes), provideHttpClient(), provideHttpClientTesting()],
      });
      http = TestBed.inject(HttpTestingController);
      harness = await RouterTestingHarness.create();
      page = await harness.navigateByUrl(registering ? '/register' : '/login', AuthPage);
    });
    afterEach(() => {
      http.verify();
      TestBed.inject(AuthService).logout();
    });
    it('invalid forms never submit; meaningful interaction shows validation', () => {
      page.submit();
      harness.detectChanges();
      http.expectNone(endpoint);
      expect(page.form.controls.username.touched).toBe(true);
      expect(harness.routeNativeElement?.querySelector('button')?.disabled).toBe(true);
      expect(harness.routeNativeElement?.querySelector('input')?.getAttribute('aria-invalid')).toBe(
        'true',
      );
    });
    it('preserves password spaces, disables duplicate submission and navigates on success', async () => {
      const navigate = vi.spyOn(TestBed.inject(Router), 'navigate');
      page.form.setValue({ username: ' demo ', password: ' password ' });
      const form = harness.routeNativeElement!.querySelector('form')!;
      form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
      harness.detectChanges();
      page.submit();
      expect(page.submitting()).toBe(true);
      expect(harness.routeNativeElement?.querySelector('fieldset')?.disabled).toBe(true);
      const request = http.expectOne(endpoint);
      expect(request.request.body.password).toBe(' password ');
      const token = `${btoa('{"alg":"HS256"}')}.${btoa(JSON.stringify({ exp: Math.floor(Date.now() / 1000) + 900 })).replace(/=/g, '')}.signature`;
      request.flush({
        userId: 1,
        username: 'demo',
        accessToken: token,
        expiresAt: new Date().toISOString(),
      });
      expect(page.submitting()).toBe(false);
      expect(page.form.controls.password.value).toBe('');
      expect(navigate).toHaveBeenCalledWith(['/tasks']);
    });
    it.each([400, registering ? 409 : 401, 0, 503])(
      'renders safe failure feedback for %s and permits retry',
      (status) => {
        page.form.setValue({ username: 'demo', password: 'Demo123!' });
        page.submit();
        http
          .expectOne(endpoint)
          .flush(
            { title: 'database SECRET internal error', errors: { password: ['SECRET'] } },
            { status, statusText: 'Failure' },
          );
        harness.detectChanges();
        expect(page.submitting()).toBe(false);
        expect(page.error()).not.toContain('SECRET');
        expect(harness.routeNativeElement?.querySelector('[role="alert"]')?.textContent).toContain(
          page.error(),
        );
        if (status === 401) expect(page.error()).toBe('Invalid username or password.');
        if (status === 409) expect(page.error()).toContain('already registered');
        if (status === 400) expect(page.error()).toContain('requirements');
      },
    );
    it('enforces backend-compatible username and password boundaries', () => {
      for (const [username, password, valid] of [
        ['ab', 'Demo123!', false],
        ['x'.repeat(65), 'Demo123!', false],
        ['demo', 'short', false],
        ['demo', ' '.repeat(8), false],
        ['a\u0001b', 'Demo123!', false],
        ['  demo  ', ' password ', true],
        ['\u0085demo\u0085', 'Demo123!', true],
        ['\ufeffab', '\ufeff'.repeat(8), true],
        ['demo', 'x'.repeat(128), true],
        ['demo', 'x'.repeat(129), false],
      ] as const) {
        page.form.setValue({ username, password });
        expect(page.form.valid).toBe(valid);
      }
    });
  });
}
