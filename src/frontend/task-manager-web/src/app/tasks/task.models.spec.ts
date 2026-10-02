import { FormControl } from '@angular/forms';
import { describe, expect, it } from 'vitest';
import {
  apiDateToInput,
  inputDateToApi,
  validCalendarDate,
  statusLabel,
  TaskStatus,
  titleValidator,
  descriptionValidator,
  statusValidator,
} from './task.models';
describe('Task contract mappings', () => {
  it('maps every numeric backend status to its UI label', () => {
    expect([0, 1, 2].map(statusLabel)).toEqual(['Pending', 'In progress', 'Completed']);
    expect(statusValidator(new FormControl('1'))).not.toBeNull();
    expect(statusValidator(new FormControl(TaskStatus.InProgress))).toBeNull();
  });
  it('round-trips calendar dates without timezone conversion', () => {
    for (const date of ['0001-01-01', '2024-02-29', '2026-10-03', '9999-12-31']) {
      expect(inputDateToApi(apiDateToInput(date))).toBe(date);
    }
  });
  it('rejects missing, impossible, timestamp and out-of-range dates', () => {
    for (const date of [
      '',
      '0000-01-01',
      '2025-02-29',
      '1900-02-29',
      '2026-04-31',
      '2026-13-01',
      '2026-01-00',
      '2026-10-03T00:00:00Z',
    ]) {
      expect(validCalendarDate(date)).toBe(false);
      expect(() => inputDateToApi(date)).toThrow();
    }
    expect(validCalendarDate('2000-02-29')).toBe(true);
  });
  it('matches trimmed UTF-16 backend text boundaries', () => {
    expect(titleValidator(new FormControl('\u0085  '))).not.toBeNull();
    expect(titleValidator(new FormControl(' ' + 'x'.repeat(120) + ' '))).toBeNull();
    expect(titleValidator(new FormControl('x'.repeat(121)))).not.toBeNull();
    expect(descriptionValidator(new FormControl(' ' + 'x'.repeat(1000) + ' '))).toBeNull();
    expect(descriptionValidator(new FormControl('x'.repeat(1001)))).not.toBeNull();
    expect(titleValidator(new FormControl('😀'.repeat(61)))).not.toBeNull();
  });
});
