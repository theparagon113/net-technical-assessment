import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed, ComponentFixture } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { API_CONFIG } from '../core/api-config';
import { TasksPage } from './tasks-page';
import { TaskResult, TaskStatus } from './task.models';

describe('Tasks page and Task API integration', () => {
  let fixture: ComponentFixture<TasksPage>;
  let page: TasksPage;
  let http: HttpTestingController;
  const url = 'http://tasks.test/api/tasks';
  const saved: TaskResult = {
    id: 7,
    userId: 42,
    title: 'Persisted title',
    description: null,
    status: TaskStatus.Pending,
    dueDate: '2024-02-29',
  };
  const input = {
    title: ' Draft ',
    description: '',
    status: TaskStatus.Pending,
    dueDate: '2024-02-29',
  };
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: API_CONFIG,
          useValue: { authApiBaseUrl: 'http://auth.test', taskApiBaseUrl: 'http://tasks.test' },
        },
      ],
    });
    fixture = TestBed.createComponent(TasksPage);
    page = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  });
  afterEach(() => http.verify());
  const list = (tasks: TaskResult[] = []) => {
    http.expectOne(url).flush(tasks);
    fixture.detectChanges();
  };
  it('shows initial loading then empty state', () => {
    expect(fixture.nativeElement.textContent).toContain('Loading tasks');
    expect(fixture.nativeElement.textContent).not.toContain('No tasks yet');
    list();
    expect(fixture.nativeElement.textContent).toContain('No tasks yet');
  });
  it('shows a safe recoverable list failure and permits retry', () => {
    http.expectOne(url).flush({ detail: 'private SQL' }, { status: 500, statusText: 'Error' });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Reload tasks to retry');
    expect(fixture.nativeElement.textContent).not.toContain('private SQL');
    page.load();
    list([saved]);
    expect(page.listError()).toBe('');
    expect(fixture.nativeElement.textContent).toContain('Persisted title');
  });
  it('validates before sending and creates from the server result without ownership input', () => {
    list();
    page.openForm();
    page.submit();
    http.expectNone(url);
    page.form.setValue(input);
    page.submit();
    page.submit();
    expect(page.busy()).toBe(true);
    expect(page.tasks()).toEqual([]);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('fieldset').disabled).toBe(true);
    const request = http.expectOne(url);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ ...input, description: null });
    request.flush(saved);
    expect(page.tasks()).toEqual([saved]);
    expect(page.formOpen()).toBe(false);
    expect(page.form.controls.title.value).toBe('');
    expect(page.feedback()).toBe('Task created.');
  });
  it('prepopulates edit, sends numeric status/date and uses persisted PUT response', () => {
    list([saved]);
    page.openForm(saved);
    expect(page.form.getRawValue()).toEqual({
      title: saved.title,
      description: '',
      status: saved.status,
      dueDate: saved.dueDate,
    });
  });
  it('retains edits and existing list on failed PUT, then retries', () => {
    list([saved]);
    page.openForm(saved);
    page.form.controls.title.setValue('Changed');
    page.form.controls.status.setValue(TaskStatus.Completed);
    page.submit();
    const request = http.expectOne(url + '/7');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({
      title: 'Changed',
      description: null,
      status: 2,
      dueDate: '2024-02-29',
    });
    request.flush({}, { status: 400, statusText: 'Bad request' });
    expect(page.tasks()).toEqual([saved]);
    expect(page.formOpen()).toBe(true);
    expect(page.form.controls.title.value).toBe('Changed');
    expect(page.feedback()).toBe('');
    page.submit();
    http.expectOne(url + '/7').flush({ ...saved, title: 'Changed', status: 2 });
    expect(page.tasks()[0].status).toBe(TaskStatus.Completed);
    expect(page.formOpen()).toBe(false);
  });
  it('requires confirmation, retains task on failed DELETE and removes only on success', () => {
    list([saved]);
    page.requestDelete(saved);
    http.expectNone(url + '/7');
    page.cancelDelete();
    page.confirmDelete();
    http.expectNone(url + '/7');
    page.requestDelete(saved);
    page.confirmDelete();
    page.confirmDelete();
    const request = http.expectOne(url + '/7');
    expect(request.request.method).toBe('DELETE');
    request.flush({}, { status: 500, statusText: 'Error' });
    expect(page.tasks()).toEqual([saved]);
    expect(page.deleting()).toEqual(saved);
    expect(page.feedback()).toBe('');
    page.confirmDelete();
    http.expectOne(url + '/7').flush(null);
    expect(page.tasks()).toEqual([]);
    expect(page.deleting()).toBeNull();
    expect(page.feedback()).toBe('Task deleted.');
  });
  it('keeps failed create form for recovery without invented task', () => {
    list();
    page.openForm();
    page.form.setValue(input);
    page.submit();
    http.expectOne(url).error(new ProgressEvent('error'));
    expect(page.tasks()).toEqual([]);
    expect(page.form.getRawValue()).toEqual(input);
    expect(page.formOpen()).toBe(true);
    expect(page.busy()).toBe(false);
    expect(page.feedback()).toBe('');
  });
  it('renders descriptions as text, status labels, and date without ownership fields', () => {
    list([{ ...saved, description: '<script>bad()</script>', status: TaskStatus.InProgress }]);
    expect(fixture.nativeElement.textContent).toContain('<script>bad()</script>');
    expect(fixture.nativeElement.querySelector('script')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('In progress');
    expect(fixture.nativeElement.querySelector('time').textContent).toBe('2024-02-29');
    expect(fixture.nativeElement.querySelector('[name=userId]')).toBeNull();
  });
  it('blocks mutation while a list reload is outstanding', () => {
    list([saved]);
    page.openForm(saved);
    page.form.controls.title.setValue('Changed');
    page.load();
    page.submit();
    http.expectNone(url + '/7');
    http.expectOne(url).flush([saved]);
    page.requestDelete(saved);
    page.load();
    page.confirmDelete();
    http.expectNone(url + '/7');
    http.expectOne(url).flush([saved]);
    expect(page.tasks()).toEqual([saved]);
  });
  it('times out an unconfirmed create without discarding the form or inventing success', () => {
    list();
    page.openForm();
    page.form.setValue(input);
    vi.useFakeTimers();
    try {
      page.submit();
      const request = http.expectOne(url);
      vi.advanceTimersByTime(10001);
      expect(request.cancelled).toBe(true);
      expect(page.busy()).toBe(false);
      expect(page.formOpen()).toBe(true);
      expect(page.tasks()).toEqual([]);
      expect(page.feedback()).toBe('');
      expect(page.actionError()).toContain('Reload to check the saved state');
    } finally {
      vi.useRealTimers();
    }
  });
});
