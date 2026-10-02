import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { finalize, Observable, TimeoutError } from 'rxjs';
import { AuthService } from '../core/auth/auth.service';
import {
  apiDateToInput,
  descriptionValidator,
  dueDateValidator,
  inputDateToApi,
  statusLabel,
  statusValidator,
  TaskResult,
  TaskStatus,
  taskStatuses,
  titleValidator,
} from './task.models';
import { TaskService } from './task.service';

@Component({
  imports: [ReactiveFormsModule],
  templateUrl: './tasks-page.html',
  styleUrl: './tasks-page.css',
})
export class TasksPage implements OnInit {
  readonly auth = inject(AuthService);
  private readonly service = inject(TaskService);
  private readonly destroyRef = inject(DestroyRef);
  readonly tasks = signal<TaskResult[]>([]);
  readonly loading = signal(false);
  readonly busy = signal(false);
  readonly loaded = signal(false);
  readonly listError = signal('');
  readonly actionError = signal('');
  readonly feedback = signal('');
  readonly formOpen = signal(false);
  readonly editing = signal<TaskResult | null>(null);
  readonly deleting = signal<TaskResult | null>(null);
  readonly statuses = taskStatuses;
  readonly statusLabel = statusLabel;
  readonly form = new FormGroup({
    title: new FormControl('', { nonNullable: true, validators: [titleValidator] }),
    description: new FormControl('', { nonNullable: true, validators: [descriptionValidator] }),
    status: new FormControl(TaskStatus.Pending, {
      nonNullable: true,
      validators: [statusValidator],
    }),
    dueDate: new FormControl('', { nonNullable: true, validators: [dueDateValidator] }),
  });
  ngOnInit(): void {
    this.load();
  }
  load(): void {
    if (this.loading() || this.busy()) return;
    this.loading.set(true);
    this.listError.set('');
    this.service
      .list()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.loading.set(false)),
      )
      .subscribe({
        next: (tasks) => {
          this.tasks.set(tasks);
          this.loaded.set(true);
        },
        error: (error) => this.listError.set(this.errorMessage(error)),
      });
  }
  openForm(task: TaskResult | null = null): void {
    if (this.busy() || this.loading()) return;
    this.editing.set(task);
    this.deleting.set(null);
    this.actionError.set('');
    this.feedback.set('');
    this.form.reset(
      task
        ? {
            title: task.title,
            description: task.description ?? '',
            status: task.status,
            dueDate: apiDateToInput(task.dueDate),
          }
        : { title: '', description: '', status: TaskStatus.Pending, dueDate: '' },
    );
    this.formOpen.set(true);
  }
  closeForm(): void {
    if (this.busy()) return;
    this.formOpen.set(false);
    this.editing.set(null);
    this.actionError.set('');
  }
  submit(): void {
    if (this.busy() || this.loading()) return;
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    const value = this.form.getRawValue();
    const input = {
      ...value,
      description: value.description || null,
      dueDate: inputDateToApi(value.dueDate),
    };
    const edit = this.editing();
    this.run(edit ? this.service.update(edit.id, input) : this.service.create(input), (saved) => {
      this.tasks.update((tasks) =>
        edit ? tasks.map((task) => (task.id === saved.id ? saved : task)) : [...tasks, saved],
      );
      this.formOpen.set(false);
      this.editing.set(null);
      this.form.reset();
      this.feedback.set(edit ? 'Task updated.' : 'Task created.');
    });
  }
  requestDelete(task: TaskResult): void {
    if (this.busy() || this.loading()) return;
    this.closeForm();
    this.feedback.set('');
    this.deleting.set(task);
  }
  cancelDelete(): void {
    if (this.busy()) return;
    this.deleting.set(null);
    this.actionError.set('');
  }
  confirmDelete(): void {
    const task = this.deleting();
    if (!task || this.busy() || this.loading()) return;
    this.run(this.service.delete(task.id), () => {
      this.tasks.update((tasks) => tasks.filter((item) => item.id !== task.id));
      this.deleting.set(null);
      this.feedback.set('Task deleted.');
    });
  }
  private run<T>(request: Observable<T>, success: (result: T) => void): void {
    this.busy.set(true);
    this.actionError.set('');
    this.feedback.set('');
    request
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.busy.set(false)),
      )
      .subscribe({
        next: success,
        error: (error) => this.actionError.set(this.errorMessage(error)),
      });
  }
  private errorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 400) return 'Check the task fields and try again.';
      if (error.status === 404)
        return 'This task is no longer available. Cancel and reload the list.';
      if (error.status === 401) return 'Your session ended. Please sign in again.';
    }
    if (error instanceof TimeoutError)
      return 'The task service took too long to respond. Reload to check the saved state before retrying.';
    return 'Unable to reach the task service. Your changes are not confirmed. Please try again or reload the list.';
  }
}
