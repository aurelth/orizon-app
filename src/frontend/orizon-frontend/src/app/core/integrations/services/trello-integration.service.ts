import { Injectable, inject } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { ApiService } from '../../http/api.service';
import { IntegrationsStore } from '../store/integrations.store';

export interface TrelloBoard {
  boardId: string;
  name: string;
  color?: string;
  lists: TrelloList[];
}

export interface TrelloList {
  listId: string;
  name: string;
  detectedType?: string;
}

export interface SaveBoardConfigRequest {
  boardId: string;
  boardName: string;
  boardColor?: string;
  todayListId?: string;
  todayListName?: string;
  inProgressListId?: string;
  inProgressListName?: string;
}

@Injectable({ providedIn: 'root' })
export class TrelloIntegrationService {
  private readonly api = inject(ApiService);
  private readonly store = inject(IntegrationsStore);

  getStatus(): Observable<{ connected: boolean }> {
    return this.api.get<{ connected: boolean }>('/trello/status').pipe(
      tap({
        next: ({ connected }) => this.store.setTrelloConnected(connected),
        error: () => { },
      })
    );
  }

  connect(apiKey: string, token: string): Observable<void> {
    this.store.setLoadingTrello(true);
    return this.api.post<void>('/trello/connect', { apiKey, token }).pipe(
      tap({
        next: () => this.store.setTrelloConnected(true),
        error: () => {
          this.store.setLoadingTrello(false);
          this.store.setError('Credenciais Trello inválidas.');
        },
      })
    );
  }

  getBoards(apiKey?: string, token?: string): Observable<TrelloBoard[]> {
    const url = apiKey && token
      ? `/trello/boards?apiKey=${apiKey}&token=${token}`
      : `/trello/boards`;

    return this.api.get<TrelloBoard[]>(url).pipe(
      tap({
        next: (boards) => this.store.setTrelloBoards(boards),
        error: () => this.store.setError('Falha ao carregar boards do Trello.'),
      })
    );
  }

  saveBoardConfig(request: SaveBoardConfigRequest): Observable<void> {
    return this.api.post<void>('/trello/boards/config', request).pipe(
      tap({
        next: () => {
          this.store.setTrelloBoardConfig({
            boardId: request.boardId,
            boardName: request.boardName,
            listIds: [request.todayListId, request.inProgressListId]
              .filter(Boolean) as string[],
          });
        },
        error: () => this.store.setError('Falha ao salvar configuração do board.'),
      })
    );
  }
}