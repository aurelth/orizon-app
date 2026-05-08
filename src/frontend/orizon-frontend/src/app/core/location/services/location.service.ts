import { Injectable, inject } from '@angular/core';
import { Observable, from, map, switchMap, tap } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { LocationStore } from '../store/location.store';
import { ApiService } from '../../http/api.service';

interface GeocodeResult {
  city: string;
  lat: number;
  lon: number;
}

@Injectable({ providedIn: 'root' })
export class LocationService {
  private readonly http = inject(HttpClient);
  private readonly store = inject(LocationStore);
  private readonly api = inject(ApiService);

  detectCurrentLocation(): Observable<GeocodeResult> {
    this.store.setDetecting(true);

    const position$ = from(
      new Promise<GeolocationPosition>((resolve, reject) =>
        navigator.geolocation.getCurrentPosition(resolve, reject)
      )
    );

    return position$.pipe(
      switchMap((pos) => {
        const { latitude: lat, longitude: lon } = pos.coords;
        return this.http.get<{ address: { city: string; town: string; village: string } }>(
          `https://nominatim.openstreetmap.org/reverse?lat=${lat}&lon=${lon}&format=json`
        ).pipe(
          switchMap((result) => {
            const city =
              result.address.city ||
              result.address.town ||
              result.address.village ||
              'Localização desconhecida';
            this.store.setLocation(city, { lat, lon });
            // ADICIONADO: salvar no backend
            return this.saveLocation(city, lat, lon).pipe(
              map(() => ({ city, lat, lon }))
            );
          })
        );
      }),
      tap({
        error: () => this.store.setError('Não foi possível detectar sua localização.'),
      })
    );
  }

  searchCity(query: string): Observable<GeocodeResult[]> {
    return this.http.get<{ display_name: string; lat: string; lon: string; address: { city: string; town: string } }[]>(
      `https://nominatim.openstreetmap.org/search?q=${encodeURIComponent(query)}&format=json&limit=5`
    ).pipe(
      map((results) =>
        results.map((r) => ({
          city: r.address?.city || r.address?.town || r.display_name,
          lat: parseFloat(r.lat),
          lon: parseFloat(r.lon),
        }))
      )
    );
  }

  getLocation(): Observable<{ locationName: string; latitude: number; longitude: number }> {
    return this.api.get<{ locationName: string; latitude: number; longitude: number }>('/location').pipe(
      tap({
        next: ({ locationName, latitude, longitude }) => {
          if (locationName) {
            this.store.setLocation(locationName, { lat: latitude, lon: longitude });
          }
        },
        error: () => { },
      })
    );
  }

  saveLocation(locationName: string, latitude: number, longitude: number): Observable<void> {
    return this.api.post<void>('/location', { locationName, latitude, longitude });
  }
}