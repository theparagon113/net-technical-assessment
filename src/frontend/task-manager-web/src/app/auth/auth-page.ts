import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
} from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize, TimeoutError } from 'rxjs';
import { AuthService } from '../core/auth/auth.service';

// .NET whitespace excludes BOM and includes U+0085; lengths count UTF-16 code units.
const boundaryWhitespace =
  /^[\u0009-\u000d\u0020\u0085\u00a0\u1680\u2000-\u200a\u2028\u2029\u202f\u205f\u3000]+|[\u0009-\u000d\u0020\u0085\u00a0\u1680\u2000-\u200a\u2028\u2029\u202f\u205f\u3000]+$/g;
export function usernameValidator(control: AbstractControl): ValidationErrors | null {
  const value = String(control.value ?? '').replace(boundaryWhitespace, '');
  return value.length >= 3 && value.length <= 64 && !/[\u0000-\u001f\u007f-\u009f]/.test(value)
    ? null
    : { username: true };
}
export function passwordValidator(control: AbstractControl): ValidationErrors | null {
  const value = String(control.value ?? '');
  return value.length >= 8 &&
    value.length <= 128 &&
    value.replace(boundaryWhitespace, '').length > 0
    ? null
    : { password: true };
}

@Component({ imports: [ReactiveFormsModule, RouterLink], templateUrl: './auth-page.html' })
export class AuthPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  readonly registering = this.route.snapshot.data['registering'] === true;
  readonly submitting = signal(false);
  readonly error = signal('');
  readonly expired = this.route.snapshot.queryParamMap.get('session') === 'expired';
  readonly form = new FormGroup({
    username: new FormControl('', { nonNullable: true, validators: [usernameValidator] }),
    password: new FormControl('', { nonNullable: true, validators: [passwordValidator] }),
  });

  submit(): void {
    if (this.submitting()) return;
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    this.error.set('');
    this.submitting.set(true);
    const input = this.form.getRawValue();
    const request = this.registering ? this.auth.register(input) : this.auth.login(input);
    request
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.submitting.set(false)),
      )
      .subscribe({
        next: () => {
          this.form.controls.password.reset();
          void this.router.navigate(['/tasks']);
        },
        error: (error) => this.error.set(this.errorMessage(error)),
      });
  }

  private errorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 401 && !this.registering) return 'Invalid username or password.';
      if (error.status === 409 && this.registering)
        return 'That username is already registered. Choose another or sign in.';
      if (error.status === 400)
        return 'Check your username and password against the requirements below.';
      if (error.status === 0 || error.status >= 500)
        return 'The authentication service is unavailable. Please try again.';
    }
    if (error instanceof TimeoutError)
      return 'The authentication service took too long to respond. Please try again.';
    return 'Unable to start your session. Check that browser session storage is enabled and try again.';
  }
}
