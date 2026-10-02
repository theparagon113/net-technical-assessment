import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { firstValueFrom } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';
import { routes } from '../app.routes';
import { authInterceptor } from '../core/auth/auth.interceptor';
import { AuthService, SESSION_KEY } from '../core/auth/auth.service';
import { TasksPage } from './tasks-page';
import { TaskService } from './task.service';
import { TaskStatus } from './task.models';

const live =
  (globalThis as typeof globalThis & { process?: { env: Record<string, string> } }).process?.env[
    'M6_LIVE'
  ] === '1';
describe.skipIf(!live)('Live Angular task CRUD through both hosts', () => {
  it('guards anonymous access, registers/logs in, round-trips all fields/statuses, restores and deletes', async () => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideRouter(routes), provideHttpClient(withInterceptors([authInterceptor]))],
    });
    const harness = await RouterTestingHarness.create('/tasks');
    expect(TestBed.inject(Router).url).toBe('/login');
    const auth = TestBed.inject(AuthService);
    const credentials = { username: 'm6-' + Date.now(), password: 'Assessment123!' };
    await firstValueFrom(auth.register(credentials));
    await auth.logout();
    await firstValueFrom(auth.login(credentials));
    const page = await harness.navigateByUrl('/tasks', TasksPage);
    await vi.waitFor(() => expect(page.loaded()).toBe(true));
    expect(page.tasks()).toEqual([]);
    page.openForm();
    page.form.setValue({
      title: ' Live create ',
      description: 'Description from Angular',
      status: TaskStatus.Pending,
      dueDate: '2024-02-29',
    });
    page.submit();
    await vi.waitFor(() => expect(page.busy()).toBe(false));
    expect(page.actionError()).toBe('');
    const saved = page.tasks()[0];
    expect(saved.id).toBeGreaterThan(0);
    expect(saved.title).toBe('Live create');
    expect(saved.description).toBe('Description from Angular');
    expect(saved.status).toBe(0);
    expect(saved.dueDate).toBe('2024-02-29');
    for (const status of [TaskStatus.InProgress, TaskStatus.Completed]) {
      page.openForm(page.tasks()[0]);
      expect(page.form.controls.dueDate.value).toBe(page.tasks()[0].dueDate);
      page.form.patchValue({ title: 'Live edit', status, dueDate: '2026-10-03' });
      page.submit();
      await vi.waitFor(() => expect(page.busy()).toBe(false));
      expect(page.actionError()).toBe('');
      expect(page.tasks()[0]).toMatchObject({
        id: saved.id,
        title: 'Live edit',
        status,
        dueDate: '2026-10-03',
      });
    }
    page.load();
    await vi.waitFor(() => expect(page.loading()).toBe(false));
    expect(page.tasks()[0].status).toBe(2);
    const token = sessionStorage.getItem(SESSION_KEY);

    expect(auth.getToken()).toBe(token);
    const task = page.tasks()[0];
    page.requestDelete(task);
    expect((await firstValueFrom(TestBed.inject(TaskService).list())).length).toBe(1);
    page.confirmDelete();
    await vi.waitFor(() => expect(page.busy()).toBe(false));
    expect(page.actionError()).toBe('');
    expect(page.tasks()).toEqual([]);
    expect(await firstValueFrom(TestBed.inject(TaskService).list())).toEqual([]);
    // Simulate deletion in another tab, then exercise the real recoverable 404 path.
    const service = TestBed.inject(TaskService);
    const stale = await firstValueFrom(
      service.create({
        title: 'Concurrent deletion',
        description: null,
        status: TaskStatus.Pending,
        dueDate: '2026-10-03',
      }),
    );
    page.load();
    await vi.waitFor(() => expect(page.loading()).toBe(false));
    await firstValueFrom(service.delete(stale.id));
    page.openForm(stale);
    page.form.controls.title.setValue('Unconfirmed edit');
    page.submit();
    await vi.waitFor(() => expect(page.busy()).toBe(false));
    expect(page.actionError()).toContain('no longer available');
    expect(page.formOpen()).toBe(true);
    expect(page.feedback()).toBe('');
    expect(page.tasks()[0].title).toBe('Concurrent deletion');
    page.closeForm();
    page.load();
    await vi.waitFor(() => expect(page.loading()).toBe(false));
    expect(page.tasks()).toEqual([]);
    await auth.logout();
    await harness.navigateByUrl('/tasks');
    expect(TestBed.inject(Router).url).toBe('/login');
    expect(sessionStorage.getItem(SESSION_KEY)).toBeNull();
  }, 30000);
});
