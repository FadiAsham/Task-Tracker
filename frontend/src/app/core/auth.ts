import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

export interface AuthSession {
  authenticated: boolean;
  name?: string;
  csrfToken: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  // Sign-in navigates the browser through Keycloak and back to the app.
  login(): void {
    window.location.assign('/api/auth/login?returnUrl=' +
      encodeURIComponent(window.location.origin + '/tasks'));
  }

  // A POST form carries the CSRF token and lets the browser follow the logout redirects.
  logout(csrfToken: string): void {
    const form = document.createElement('form');
    form.method = 'POST';
    form.action = '/api/auth/logout';
    const token = document.createElement('input');
    token.type = 'hidden';
    token.name = '__RequestVerificationToken';
    token.value = csrfToken;
    form.appendChild(token);
    document.body.appendChild(form);
    form.submit();
  }

  checkAuth() {
    return this.http.get<AuthSession>('/api/auth/me');
  }
}
