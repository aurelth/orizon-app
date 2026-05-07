import { Injectable, inject } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { ApiService } from '../../http/api.service';
import { BriefingStore } from '../store/briefing.store';
import { AuthService } from '../../auth/services/auth.service';
import { BriefingResult } from '../models/briefing.model';

@Injectable({ providedIn: 'root' })
export class BriefingService {
  private readonly api = inject(ApiService);
  private readonly store = inject(BriefingStore);
  private readonly authService = inject(AuthService);

  getTodayBriefing(): Observable<BriefingResult> {
    this.store.setLoading(true);
    return this.api.get<BriefingResult>('/briefings/today').pipe(
      tap({
        next: (briefing) => this.store.setBriefing(briefing),
        error: () => this.store.setError('Briefing de hoje não encontrado.'),
      })
    );
  }

  getBriefingByDate(date: string): Observable<BriefingResult> {
    this.store.setLoading(true);
    return this.api.get<BriefingResult>(`/briefings/${date}`).pipe(
      tap({
        next: (briefing) => this.store.setBriefing(briefing),
        error: () => this.store.setError('Briefing não encontrado para esta data.'),
      })
    );
  }

  connectSignalR(hubUrl: string): void {
    this.store.setConnecting(true);

    const token = this.authService.getAccessToken();
    const url = `${hubUrl}?access_token=${token}`;

    // importação dinâmica para não aumentar o bundle inicial
    import('@microsoft/signalr').then(({ HubConnectionBuilder, LogLevel }) => {
      const connection = new HubConnectionBuilder()
        .withUrl(url)
        .withAutomaticReconnect()
        .configureLogging(LogLevel.Warning)
        .build();

      connection.on('BriefingReady', () => {
        this.getTodayBriefing().subscribe();
      });

      connection.start()
        .then(() => this.store.setConnecting(false))
        .catch(() => this.store.setConnecting(false));
    });
  }
}