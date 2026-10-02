import { AbstractControl, ValidationErrors } from '@angular/forms';

export enum TaskStatus {
  Pending = 0,
  InProgress = 1,
  Completed = 2,
}
export const taskStatuses = [
  { value: TaskStatus.Pending, label: 'Pending' },
  { value: TaskStatus.InProgress, label: 'In progress' },
  { value: TaskStatus.Completed, label: 'Completed' },
] as const;
export function statusLabel(status: TaskStatus): string {
  return taskStatuses.find((option) => option.value === status)?.label ?? 'Unknown status';
}
export interface TaskRequest {
  title: string;
  description: string | null;
  status: TaskStatus;
  dueDate: string;
}
export interface TaskResult extends TaskRequest {
  id: number;
  userId: number;
}

// DateOnly and HTML date inputs share a calendar string. Never convert through Date/UTC.
export function validCalendarDate(value: string): boolean {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) return false;
  const [year, month, day] = value.split('-').map(Number);
  const leap = year % 4 === 0 && (year % 100 !== 0 || year % 400 === 0);
  const days = [31, leap ? 29 : 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];
  return (
    year >= 1 && year <= 9999 && month >= 1 && month <= 12 && day >= 1 && day <= days[month - 1]
  );
}
export function apiDateToInput(value: string): string {
  if (!validCalendarDate(value)) throw new Error('Invalid calendar date');
  return value;
}
export function inputDateToApi(value: string): string {
  return apiDateToInput(value);
}
export function dueDateValidator(control: AbstractControl): ValidationErrors | null {
  return validCalendarDate(String(control.value ?? '')) ? null : { dueDate: true };
}
// Match .NET Trim, including U+0085 and excluding BOM; count UTF-16 units.
export function trimTaskText(value: string): string {
  return value.replace(
    /^[\u0009-\u000d\u0020\u0085\u00a0\u1680\u2000-\u200a\u2028\u2029\u202f\u205f\u3000]+|[\u0009-\u000d\u0020\u0085\u00a0\u1680\u2000-\u200a\u2028\u2029\u202f\u205f\u3000]+$/g,
    '',
  );
}
export function titleValidator(control: AbstractControl): ValidationErrors | null {
  const length = trimTaskText(String(control.value ?? '')).length;
  return length > 0 && length <= 120 ? null : { title: true };
}
export function descriptionValidator(control: AbstractControl): ValidationErrors | null {
  return trimTaskText(String(control.value ?? '')).length <= 1000 ? null : { description: true };
}
export function statusValidator(control: AbstractControl): ValidationErrors | null {
  return taskStatuses.some((option) => option.value === control.value) ? null : { status: true };
}
