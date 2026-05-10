import { signalStore, withState, withMethods, patchState } from '@ngrx/signals';

interface TrelloList {
  listId: string;
  name: string;
  detectedType?: string;
}

interface TrelloBoard {
  boardId: string;
  name: string;
  color?: string;
  lists: TrelloList[];
}

interface TrelloBoardConfig {
  boardId: string;
  boardName: string;
  listIds: string[];
}

interface IntegrationsState {
  googleConnected: boolean;
  trelloConnected: boolean;
  trelloBoards: TrelloBoard[];
  trelloBoardConfig: TrelloBoardConfig | null;
  activeBoardId: string | null;
  isLoadingGoogle: boolean;
  isLoadingTrello: boolean;
  error: string | null;
}

const initialState: IntegrationsState = {
  googleConnected: false,
  trelloConnected: false,
  trelloBoards: [],
  trelloBoardConfig: null,
  activeBoardId: null,
  isLoadingGoogle: false,
  isLoadingTrello: false,
  error: null,
};

export const IntegrationsStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withMethods((store) => ({
    setGoogleConnected(connected: boolean): void {
      patchState(store, { googleConnected: connected, isLoadingGoogle: false });
    },
    setTrelloConnected(connected: boolean): void {
      patchState(store, { trelloConnected: connected, isLoadingTrello: false });
    },
    setTrelloBoards(boards: TrelloBoard[]): void {
      patchState(store, { trelloBoards: boards });
    },
    setTrelloBoardConfig(config: TrelloBoardConfig): void {
      patchState(store, { trelloBoardConfig: config });
    },
    setActiveBoardId(boardId: string): void {
      patchState(store, { activeBoardId: boardId });
    },
    setLoadingGoogle(loading: boolean): void {
      patchState(store, { isLoadingGoogle: loading });
    },
    setLoadingTrello(loading: boolean): void {
      patchState(store, { isLoadingTrello: loading });
    },
    setError(error: string): void {
      patchState(store, { error, isLoadingGoogle: false, isLoadingTrello: false });
    },
    clearError(): void {
      patchState(store, { error: null });
    },
  }))
);