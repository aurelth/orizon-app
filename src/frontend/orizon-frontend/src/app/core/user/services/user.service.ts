import { Injectable, inject } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { ApiService } from '../../http/api.service';
import { UserStore } from '../store/user.store';
import { UserProfile } from '../models/user.model';

export interface UpdateProfileRequest {
  displayName: string;
  profilePictureUrl: string | null;
  themePreference: string | null;
}

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly api = inject(ApiService);
  private readonly store = inject(UserStore);

  getProfile(): Observable<UserProfile> {
    this.store.setLoading(true);
    return this.api.get<UserProfile>('/users/profile').pipe(
      tap({
        next: (profile) => {
          this.store.setProfile(profile);
          this.applyTheme(profile.themePreference);
        },
        error: () => this.store.setError('Falha ao carregar perfil.'),
      })
    );
  }

  updateProfile(request: UpdateProfileRequest): Observable<void> {
    return this.api.put<void>('/users/profile', request).pipe(
      tap({
        next: () => {
          if (request.themePreference === 'Dark' || request.themePreference === 'Light') {
            this.store.updateTheme(request.themePreference);
            this.applyTheme(request.themePreference);
          }
        },
        error: () => this.store.setError('Falha ao atualizar perfil.'),
      })
    );
  }

  private applyTheme(theme: string): void {
    if (theme === 'Light') {
      document.documentElement.classList.add('theme-light');
    } else {
      document.documentElement.classList.remove('theme-light');
    }
  }
}