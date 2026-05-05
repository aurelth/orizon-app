import { signalStore, withState, withMethods, patchState } from '@ngrx/signals';

interface Coordinates {
  lat: number;
  lon: number;
}

interface LocationState {
  city: string | null;
  coordinates: Coordinates | null;
  travelMode: boolean;
  travelCity: string | null;
  travelCoordinates: Coordinates | null;
  isDetecting: boolean;
  error: string | null;
}

const initialState: LocationState = {
  city: null,
  coordinates: null,
  travelMode: false,
  travelCity: null,
  travelCoordinates: null,
  isDetecting: false,
  error: null,
};

export const LocationStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withMethods((store) => ({
    setLocation(city: string, coordinates: Coordinates): void {
      patchState(store, { city, coordinates, isDetecting: false, error: null });
    },

    setDetecting(detecting: boolean): void {
      patchState(store, { isDetecting: detecting });
    },

    enableTravelMode(city: string, coordinates: Coordinates): void {
      patchState(store, {
        travelMode: true,
        travelCity: city,
        travelCoordinates: coordinates,
      });
    },

    disableTravelMode(): void {
      patchState(store, {
        travelMode: false,
        travelCity: null,
        travelCoordinates: null,
      });
    },

    setError(error: string): void {
      patchState(store, { error, isDetecting: false });
    },

    clearError(): void {
      patchState(store, { error: null });
    },
  }))
);