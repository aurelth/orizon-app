import { signalStore, withState, withMethods, patchState } from '@ngrx/signals';
import { BriefingResult } from '../models/briefing.model';

interface BriefingState {
  briefing: BriefingResult | null;
  isLoading: boolean;
  isConnecting: boolean;
  error: string | null;
  lastUpdated: string | null;
}

const initialState: BriefingState = {
  briefing: null,
  isLoading: false,
  isConnecting: false,
  error: null,
  lastUpdated: null,
};

export const BriefingStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withMethods((store) => ({
    setBriefing(briefing: BriefingResult): void {
      patchState(store, {
        briefing,
        isLoading: false,
        error: null,
        lastUpdated: new Date().toISOString(),
      });
    },

    setLoading(isLoading: boolean): void {
      patchState(store, { isLoading });
    },

    setConnecting(isConnecting: boolean): void {
      patchState(store, { isConnecting });
    },

    setError(error: string): void {
      patchState(store, { error, isLoading: false });
    },

    clearError(): void {
      patchState(store, { error: null });
    },
  }))
);