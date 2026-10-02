import { Component, inject } from '@angular/core';
import { AuthService } from '../core/auth/auth.service';

@Component({
  template: ` <section class="panel" aria-labelledby="session-title">
    <p class="eyebrow">Signed in</p>
    <h1 id="session-title">Welcome, {{ auth.currentUser()?.username }}</h1>
    <p class="intro">
      Your session is active. Task management will be available in the next milestone.
    </p>
    <button type="button" (click)="auth.logout()">Sign out</button>
  </section>`,
})
export class SessionPage {
  readonly auth = inject(AuthService);
}
