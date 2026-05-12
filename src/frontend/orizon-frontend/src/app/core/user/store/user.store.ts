import { signalStore, withState, withMethods, patchState } from '@ngrx/signals';
import { UserProfile } from '../models/user.model';

interface UserState {
  profile: UserProfile | null;
  isLoading: boolean;
  error: string | null;
}

const initialState: UserState = {
  profile: null,
  isLoading: false,
  error: null,
};

export const UserStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withMethods((store) => ({
    setProfile(profile: UserProfile): void {
      patchState(store, { profile, isLoading: false });
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
    updateTheme(theme: 'Dark' | 'Light'): void {
      if (!store.profile()) return;
      patchState(store, {
        profile: { ...store.profile()!, themePreference: theme }
      });
    },
  }))
);