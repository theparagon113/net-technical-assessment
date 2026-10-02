import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth/auth.guard';
import { AuthPage } from './auth/auth-page';
import { TasksPage } from './tasks/tasks-page';

export const routes: Routes = [
  { path: 'login', component: AuthPage, canActivate: [guestGuard], data: { registering: false } },
  { path: 'register', component: AuthPage, canActivate: [guestGuard], data: { registering: true } },
  { path: 'tasks', component: TasksPage, canActivate: [authGuard] },
  { path: '', pathMatch: 'full', redirectTo: 'tasks' },
  { path: '**', redirectTo: 'tasks' },
];
