import { Injectable, inject } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { ApiService } from '../../http/api.service';
import { IntegrationsStore } from '../store/integrations.store';

@Injectable({ providedIn: 'root' })
export class GoogleIntegrationService {
  private readonly api = inject(ApiService);
  private readonly store = inject(IntegrationsStore);

  getAuthUrl(): Observable<{ url: string }> {
    this.store.setLoadingGoogle(true);
    return this.api.get<{ url: string }>('/google/auth-url').pipe(
      tap({
        error: () => this.store.setError('Falha ao obter URL de autenticação Google.'),
      })
    );
  }

  redirectToGoogle(): void {
    this.getAuthUrl().subscribe({
      next: ({ url }) => window.location.href = url,
      error: () => this.store.setLoadingGoogle(false),
    });
  }

  getStatus(): Observable<{ connected: boolean }> {
    return this.api.get<{ connected: boolean }>('/google/status').pipe(
      tap({
        next: ({ connected }) => this.store.setGoogleConnected(connected),
        error: () => { },
      })
    );
  }
}