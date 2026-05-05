import { TestBed } from '@angular/core/testing';
import { provideHttpClient, HttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { LocationService } from './location.service';
import { LocationStore } from '../store/location.store';
import { of } from 'rxjs';

describe('LocationService', () => {
  let service: LocationService;
  let httpClient: jest.Mocked<Partial<HttpClient>>;
  let store: InstanceType<typeof LocationStore>;

  const mockGeolocation = { getCurrentPosition: jest.fn() };

  beforeEach(() => {
    Object.defineProperty(global.navigator, 'geolocation', {
      value: mockGeolocation,
      writable: true,
      configurable: true,
    });

    httpClient = { get: jest.fn() };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        LocationStore,
        { provide: HttpClient, useValue: httpClient },
      ],
    });

    service = TestBed.inject(LocationService);
    store = TestBed.inject(LocationStore);
  });

  it('deve ser criado', () => {
    expect(service).toBeTruthy();
  });

  it('deve ativar isDetecting ao iniciar detecção', () => {
    mockGeolocation.getCurrentPosition.mockImplementation(() => {});
    service.detectCurrentLocation().subscribe({ error: () => {} });
    expect(store.isDetecting()).toBe(true);
  });

  // ALTERADO: usando done callback do Jest para aguardar resolução assíncrona da Promise
  it('deve retornar cidade usando address.city quando disponível', (done) => {
    mockGeolocation.getCurrentPosition.mockImplementation((success: PositionCallback) =>
      success({ coords: { latitude: -26.9194, longitude: -49.0661 } } as GeolocationPosition)
    );
    (httpClient.get as jest.Mock).mockReturnValue(
      of({ address: { city: 'Blumenau', town: '', village: '' } })
    );

    service.detectCurrentLocation().subscribe({
      next: (result) => {
        expect(result.city).toBe('Blumenau');
        expect(store.city()).toBe('Blumenau');
        done();
      },
    });
  });

  it('deve usar address.town quando city está vazio', (done) => {
    mockGeolocation.getCurrentPosition.mockImplementation((success: PositionCallback) =>
      success({ coords: { latitude: -26.9194, longitude: -49.0661 } } as GeolocationPosition)
    );
    (httpClient.get as jest.Mock).mockReturnValue(
      of({ address: { city: '', town: 'Pomerode', village: '' } })
    );

    service.detectCurrentLocation().subscribe({
      next: () => {
        expect(store.city()).toBe('Pomerode');
        done();
      },
    });
  });

  it('deve usar address.village quando city e town estão vazios', (done) => {
    mockGeolocation.getCurrentPosition.mockImplementation((success: PositionCallback) =>
      success({ coords: { latitude: -26.9194, longitude: -49.0661 } } as GeolocationPosition)
    );
    (httpClient.get as jest.Mock).mockReturnValue(
      of({ address: { city: '', town: '', village: 'Apiúna' } })
    );

    service.detectCurrentLocation().subscribe({
      next: () => {
        expect(store.city()).toBe('Apiúna');
        done();
      },
    });
  });

  it('deve usar fallback quando todos os campos de address estão vazios', (done) => {
    mockGeolocation.getCurrentPosition.mockImplementation((success: PositionCallback) =>
      success({ coords: { latitude: -26.9194, longitude: -49.0661 } } as GeolocationPosition)
    );
    (httpClient.get as jest.Mock).mockReturnValue(
      of({ address: { city: '', town: '', village: '' } })
    );

    service.detectCurrentLocation().subscribe({
      next: () => {
        expect(store.city()).toBe('Localização desconhecida');
        done();
      },
    });
  });

  it('deve buscar cidades por query e retornar resultados mapeados', () => {
    (httpClient.get as jest.Mock).mockReturnValue(of([
      {
        display_name: 'Blumenau, SC, Brasil',
        lat: '-26.9194',
        lon: '-49.0661',
        address: { city: 'Blumenau', town: '' },
      },
      {
        display_name: 'Joinville, SC, Brasil',
        lat: '-26.3044',
        lon: '-48.8487',
        address: { city: '', town: 'Joinville' },
      },
    ]));

    let results: { city: string; lat: number; lon: number }[] | undefined;
    service.searchCity('Blumenau').subscribe((r) => (results = r));

    expect(results).toHaveLength(2);
    expect(results?.[0].city).toBe('Blumenau');
    expect(results?.[0].lat).toBe(-26.9194);
    expect(results?.[1].city).toBe('Joinville');
  });

  it('deve usar display_name quando address.city e address.town estão ausentes', () => {
    (httpClient.get as jest.Mock).mockReturnValue(of([
      {
        display_name: 'Localidade Remota, Brasil',
        lat: '-10.0',
        lon: '-50.0',
        address: {},
      },
    ]));

    let results: { city: string }[] | undefined;
    service.searchCity('remota').subscribe((r) => (results = r));

    expect(results?.[0].city).toBe('Localidade Remota, Brasil');
  });
});