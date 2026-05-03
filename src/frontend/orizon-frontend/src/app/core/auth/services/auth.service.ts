import { Injectable, inject } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { ApiService } from '../../http/api.service';
import { AuthStore } from '../store/auth.store';
import { AuthResponse } from '../models/auth-response.model';
import { User } from '../models/user.model';

export interface RegisterRequest {
  displayName: string;
  email: string;
  password: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(ApiService);
  private readonly authStore = inject(AuthStore);

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.api.post<AuthResponse>('/auth/register', request).pipe(
      tap((response) => this.handleAuthResponse(response))
    );
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.api.post<AuthResponse>('/auth/login', request).pipe(
      tap((response) => this.handleAuthResponse(response))
    );
  }

  refresh(refreshToken: string): Observable<AuthResponse> {
    return this.api.post<AuthResponse>('/auth/refresh', { refreshToken }).pipe(
      tap((response) => this.handleAuthResponse(response))
    );
  }

  logout(): void {
    const refreshToken = localStorage.getItem('refresh_token');
    if (refreshToken) {
      this.api.post('/auth/logout', { refreshToken }).subscribe();
    }
    this.authStore.logout();
  }

  getAccessToken(): string | null {
    return localStorage.getItem('access_token');
  }

  getRefreshToken(): string | null {
    return localStorage.getItem('refresh_token');
  }

  isAuthenticated(): boolean {
    return !!this.getAccessToken();
  }

  private handleAuthResponse(response: AuthResponse): void {
    const user: User = {
      id: '',
      email: response.email,
      displayName: response.displayName,
      timezone: 'UTC',
      themePreference: 'dark',
    };
    this.authStore.setAuth(response.accessToken, response.refreshToken, user);
  }
}