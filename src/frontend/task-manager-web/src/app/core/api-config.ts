import { InjectionToken } from '@angular/core';

export interface ApiConfig {
  authApiBaseUrl: string;
  taskApiBaseUrl: string;
}
// Public environment URLs only. Backend secrets never belong in this file.
export const apiConfig: ApiConfig = {
  authApiBaseUrl: 'http://localhost:5150',
  taskApiBaseUrl: 'http://localhost:5149',
};
export const API_CONFIG = new InjectionToken<ApiConfig>('API_CONFIG', {
  providedIn: 'root',
  factory: () => apiConfig,
});
