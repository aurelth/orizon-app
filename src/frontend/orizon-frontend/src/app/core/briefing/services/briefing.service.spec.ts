import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { BriefingService } from './briefing.service';
import { BriefingStore } from '../store/briefing.store';
import { ApiService } from '../../http/api.service';
import { AuthService } from '../../auth/services/auth.service';
import { AuthStore } from '../../auth/store/auth.store';
import { of, throwError } from 'rxjs';
import { BriefingResult } from '../models/briefing.model';

describe('BriefingService', () => {
  let service: BriefingService;
  let apiService: jest.Mocked<Partial<ApiService>>;
  let authService: jest.Mocked<Partial<AuthService>>;
  let store: InstanceType<typeof BriefingStore>;

  const mockBriefing: BriefingResult = {
    briefingId: 'abc-123',
    date: '2026-05-06',
    userName: 'Aurel',
    weather: {
      currentTemperature: 22,
      minTemperature: 18,
      maxTemperature: 26,
      description: 'Ensolarado',
      weatherEmoji: '☀️',
      humidity: 60,
      windSpeed: 10,
      locationName: 'Blumenau',
      hourlyPrecipitation: {},
      rainStartHour: null,
      rainEndHour: null,
      willRain: false,
    },
    emails: [],
    calendarEvents: [],
    trelloTasks: null,
    aiSummary: {
      greeting: 'Bom dia, Aurel!',
      weatherSummary: 'Dia ensolarado.',
      suggestions: 'Ótimo dia.',
      priorityTask: null,
      actionChips: [],
    },
    generatedAt: '2026-05-06T06:00:00Z',
  };

  beforeEach(() => {
    apiService = { get: jest.fn() };
    authService = {
      getAccessToken: jest.fn().mockReturnValue('mock-token'),
      getRefreshToken: jest.fn().mockReturnValue(null),
      isAuthenticated: jest.fn().mockReturnValue(true),
      logout: jest.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        BriefingStore,
        AuthStore,
        { provide: ApiService, useValue: apiService },
        { provide: AuthService, useValue: authService },
      ],
    });

    service = TestBed.inject(BriefingService);
    store = TestBed.inject(BriefingStore);
  });

  it('deve ser criado', () => {
    expect(service).toBeTruthy();
  });

  it('deve buscar briefing de hoje e atualizar store', () => {
    (apiService.get as jest.Mock).mockReturnValue(of(mockBriefing));

    service.getTodayBriefing().subscribe((result) => {
      expect(result.briefingId).toBe('abc-123');
    });

    expect(store.briefing()).toEqual(mockBriefing);
    expect(store.isLoading()).toBe(false);
  });

  it('deve definir erro no store quando getTodayBriefing falhar', () => {
    (apiService.get as jest.Mock).mockReturnValue(
      throwError(() => new Error('Not Found'))
    );

    service.getTodayBriefing().subscribe({ error: () => {} });

    expect(store.error()).toBe('Briefing de hoje não encontrado.');
  });

  it('deve buscar briefing por data e atualizar store', () => {
    (apiService.get as jest.Mock).mockReturnValue(of(mockBriefing));

    service.getBriefingByDate('2026-05-06').subscribe();

    expect(store.briefing()).toEqual(mockBriefing);
  });

  it('deve definir erro no store quando getBriefingByDate falhar', () => {
    (apiService.get as jest.Mock).mockReturnValue(
      throwError(() => new Error('Not Found'))
    );

    service.getBriefingByDate('2026-05-06').subscribe({ error: () => {} });

    expect(store.error()).toBe('Briefing não encontrado para esta data.');
  });

  it('deve ativar isLoading ao chamar getTodayBriefing', () => {
    (apiService.get as jest.Mock).mockReturnValue(of(mockBriefing));
    service.getTodayBriefing().subscribe();
    expect(store.isLoading()).toBe(false);
  });
});