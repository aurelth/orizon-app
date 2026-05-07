import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { DashboardComponent } from './dashboard';
import { BriefingStore } from '../../core/briefing/store/briefing.store';
import { BriefingService } from '../../core/briefing/services/briefing.service';
import { AuthStore } from '../../core/auth/store/auth.store';
import { AuthService } from '../../core/auth/services/auth.service';
import { of } from 'rxjs';
import { BriefingResult } from '../../core/briefing/models/briefing.model';

describe('DashboardComponent', () => {
  let component: DashboardComponent;
  let briefingService: jest.Mocked<Partial<BriefingService>>;
  let authService: jest.Mocked<Partial<AuthService>>;

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

  beforeEach(async () => {
    briefingService = {
      getTodayBriefing: jest.fn().mockReturnValue(of(mockBriefing)),
      connectSignalR: jest.fn(),
    };

    authService = {
      getAccessToken: jest.fn().mockReturnValue('mock-token'),
      getRefreshToken: jest.fn().mockReturnValue(null),
      isAuthenticated: jest.fn().mockReturnValue(true),
      logout: jest.fn(),
    };

    await TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        BriefingStore,
        AuthStore,
        { provide: BriefingService, useValue: briefingService },
        { provide: AuthService, useValue: authService },
      ],
    }).compileComponents();

    component = TestBed.runInInjectionContext(() => new DashboardComponent());
  });

  it('deve ser criado', () => {
    expect(component).toBeTruthy();
  });

  it('deve chamar getTodayBriefing no ngOnInit', () => {
    component.ngOnInit();
    expect(briefingService.getTodayBriefing).toHaveBeenCalled();
  });

  it('deve chamar connectSignalR no ngOnInit', () => {
    component.ngOnInit();
    expect(briefingService.connectSignalR).toHaveBeenCalled();
  });
});