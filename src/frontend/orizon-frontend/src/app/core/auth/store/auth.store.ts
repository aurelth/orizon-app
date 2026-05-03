import { signalStore, withState, withMethods, patchState } from '@ngrx/signals';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { User } from '../models/user.model';

interface AuthState {
  user: User | null;
  accessToken: string | null;
  refreshToken: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
}

const initialState: AuthState = {
  user: null,
  accessToken: null,
  refreshToken: null,
  isAuthenticated: false,
  isLoading: false,
  error: null,
};

export const AuthStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withMethods((store) => {
    const router = inject(Router);

    return {
      setAuth(accessToken: string, refreshToken: string, user: User): void {
        localStorage.setItem('access_token', accessToken);
        localStorage.setItem('refresh_token', refreshToken);
        patchState(store, {
          accessToken,
          refreshToken,
          user,
          isAuthenticated: true,
          error: null,
        });
      },

      setLoading(isLoading: boolean): void {
        patchState(store, { isLoading });
      },

      setError(error: string): void {
        patchState(store, { error, isLoading: false });
      },

      clearError(): void {
        patchState(store, { error: null });
      },

      logout(): void {
        localStorage.removeItem('access_token');
        localStorage.removeItem('refresh_token');
        patchState(store, initialState);
        router.navigate(['/auth/login']);
      },

      loadFromStorage(): void {
        const accessToken = localStorage.getItem('access_token');
        const refreshToken = localStorage.getItem('refresh_token');
        if (accessToken && refreshToken) {
          patchState(store, {
            accessToken,
            refreshToken,
            isAuthenticated: true,
          });
        }
      },
    };
  })
);