import { Injectable, inject } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { ApiService } from '../../http/api.service';
import { BriefingStore } from '../store/briefing.store';
import { AuthService } from '../../auth/services/auth.service';
import { BriefingResult } from '../models/briefing.model';

export interface BriefingHistoryItem {
  briefingId: string;
  date: string;
  status: string;
  greeting: string;
  generatedAt: string;
}

export interface BriefingHistoryResult {
  items: BriefingHistoryItem[];
  page: number;
  pageSize: number;
  total: number;
  totalPages: number;
}

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

  getHistory(page = 1, pageSize = 10): Observable<BriefingHistoryResult> {
    return this.api.get<BriefingHistoryResult>(
      `/briefings/history?page=${page}&pageSize=${pageSize}`
    );
  }

  connectSignalR(hubUrl: string): void {
    this.store.setConnecting(true);

    const token = this.authService.getAccessToken();
    const url = `${hubUrl}?access_token=${token}`;

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

  generateBriefing(): Observable<{ jobId: string; message: string }> {
    return this.api.post<{ jobId: string; message: string }>('/briefings/generate', {}).pipe(
      tap({
        next: () => {
          setTimeout(() => this.getTodayBriefing().subscribe(), 15000);
        },
        error: () => { },
      })
    );
  }
}