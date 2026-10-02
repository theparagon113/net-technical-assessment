import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth/auth.guard';
import { AuthPage } from './auth/auth-page';
import { SessionPage } from './auth/session-page';

export const routes: Routes = [
  { path: 'login', component: AuthPage, canActivate: [guestGuard], data: { registering: false } },
  { path: 'register', component: AuthPage, canActivate: [guestGuard], data: { registering: true } },
  { path: 'tasks', component: SessionPage, canActivate: [authGuard] },
  { path: '', pathMatch: 'full', redirectTo: 'tasks' },
  { path: '**', redirectTo: 'tasks' },
];
