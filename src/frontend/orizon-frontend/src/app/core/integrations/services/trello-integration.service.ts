import { Injectable, inject } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { ApiService } from '../../http/api.service';
import { IntegrationsStore } from '../store/integrations.store';

export interface TrelloBoard {
  id: string;
  name: string;
}

export interface TrelloList {
  id: string;
  name: string;
}

export interface SaveBoardConfigRequest {
  boardId: string;
  listIds: string[];
}

@Injectable({ providedIn: 'root' })
export class TrelloIntegrationService {
  private readonly api = inject(ApiService);
  private readonly store = inject(IntegrationsStore);

  connect(apiKey: string, token: string): Observable<void> {
    this.store.setLoadingTrello(true);
    return this.api.post<void>('/trello/connect', { apiKey, token }).pipe(
      tap({
        next: () => this.store.setTrelloConnected(true),
        error: () => this.store.setError('Credenciais Trello inválidas.'),
      })
    );
  }

  getBoards(): Observable<TrelloBoard[]> {
    return this.api.get<TrelloBoard[]>('/trello/boards').pipe(
      tap({
        next: (boards) => this.store.setTrelloBoards(boards),
        error: () => this.store.setError('Falha ao carregar boards do Trello.'),
      })
    );
  }

  saveBoardConfig(request: SaveBoardConfigRequest): Observable<void> {
    return this.api.post<void>('/trello/board-config', request).pipe(
      tap({
        next: () => {
          this.store.setTrelloBoardConfig({
            boardId: request.boardId,
            boardName: '',
            listIds: request.listIds,
          });
        },
        error: () => this.store.setError('Falha ao salvar configuração do board.'),
      })
    );
  }
}