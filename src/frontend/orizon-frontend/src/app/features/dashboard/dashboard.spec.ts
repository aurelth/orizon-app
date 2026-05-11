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
    emails: [{ from: 'a@b.com', subject: 'Teste', aiSummary: '', category: 'Info', categoryEmoji: '📧', receivedAt: '' }],
    calendarEvents: [{ title: 'Reunião', startTime: '', endTime: '', participants: [], meetLink: null, description: null, conflictsWithRain: false }],
    trelloTasks: [{ cardId: '1', title: 'Task', boardName: 'Board', boardColor: '#fff', listName: 'Today', columnType: 'today', isStuck: false, daysInProgress: null, movedToInProgressAt: null }],
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
      generateBriefing: jest.fn().mockReturnValue(of({ jobId: '1', message: 'ok' })),
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

    store = TestBed.inject(BriefingStore);
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

  it('deve retornar undefined para weather quando briefing é null', () => {
    expect(component.weather()).toBeUndefined();
  });

  it('deve retornar weather quando briefing está carregado', () => {
    store.setBriefing(mockBriefing);
    expect(component.weather()).toEqual(mockBriefing.weather);
  });

  it('deve retornar array vazio para emails quando briefing é null', () => {
    expect(component.emails()).toEqual([]);
  });

  it('deve retornar emails quando briefing está carregado', () => {
    store.setBriefing(mockBriefing);
    expect(component.emails()).toHaveLength(1);
  });

  it('deve retornar array vazio para calendarEvents quando briefing é null', () => {
    expect(component.calendarEvents()).toEqual([]);
  });

  it('deve retornar calendarEvents quando briefing está carregado', () => {
    store.setBriefing(mockBriefing);
    expect(component.calendarEvents()).toHaveLength(1);
  });

  it('deve retornar null para trelloTasks quando briefing é null', () => {
    expect(component.trelloTasks()).toBeNull();
  });

  it('deve retornar trelloTasks quando briefing está carregado', () => {
    store.setBriefing(mockBriefing);
    expect(component.trelloTasks()).toHaveLength(1);
  });

  it('deve retornar undefined para aiSummary quando briefing é null', () => {
    expect(component.aiSummary()).toBeUndefined();
  });

  it('deve retornar aiSummary quando briefing está carregado', () => {
    store.setBriefing(mockBriefing);
    expect(component.aiSummary()).toEqual(mockBriefing.aiSummary);
  });

  it('deve chamar generateBriefing e setar isGenerating', () => {
    component.generateBriefing();
    expect(briefingService.generateBriefing).toHaveBeenCalled();
  });

  it('deve formatar data corretamente', () => {
    const result = component.formatDate('2026-05-09');
    expect(result).toContain('Maio');
    expect(result).toContain('9');
  });
});