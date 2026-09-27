import { Component, inject, signal } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { AuthService, AuthSession } from './core/auth';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  readonly auth = inject(AuthService);
  readonly session = signal<AuthSession | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');

  constructor() { this.refreshSession(); }

  refreshSession(): void {
    this.loading.set(true);
    this.error.set(new URLSearchParams(window.location.search).has('authError')
      ? 'Sign-in failed. Check the identity provider configuration and try again.' : '');
    this.auth.checkAuth().subscribe({
      next: session => { this.session.set(session); this.loading.set(false); },
      error: () => {
        this.error.set('Unable to check your session. Check that the API is running, then retry.');
        this.loading.set(false);
      },
    });
  }
}
