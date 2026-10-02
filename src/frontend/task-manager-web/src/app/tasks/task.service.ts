import { HttpClient } from '@angular/common/http';
import { timeout } from 'rxjs';
import { inject, Injectable } from '@angular/core';
import { API_CONFIG } from '../core/api-config';
import { TaskRequest, TaskResult } from './task.models';
@Injectable({ providedIn: 'root' })
export class TaskService {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_CONFIG).taskApiBaseUrl}/api/tasks`;
  list() {
    return this.http.get<TaskResult[]>(this.url).pipe(timeout(10000));
  }
  create(input: TaskRequest) {
    return this.http.post<TaskResult>(this.url, input).pipe(timeout(10000));
  }
  update(id: number, input: TaskRequest) {
    return this.http.put<TaskResult>(`${this.url}/${id}`, input).pipe(timeout(10000));
  }
  delete(id: number) {
    return this.http.delete<void>(`${this.url}/${id}`).pipe(timeout(10000));
  }
}
