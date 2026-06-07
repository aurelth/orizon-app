import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, ActivatedRoute, Router } from '@angular/router';
import { HistoryDetailComponent } from './history-detail';
import { BriefingService } from '../../../core/briefing/services/briefing.service';
import { of, throwError } from 'rxjs';
import { BriefingResult } from '../../../core/briefing/models/briefing.model';

describe('HistoryDetailComponent', () => {
  let component: HistoryDetailComponent;
  let briefingService: jest.Mocked<Partial<BriefingService>>;
  let router: Router;

  const mockBriefing: BriefingResult = {
    briefingId: 'b1',
    date: '2026-05-10',
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
    googleTasks: null,
    aiSummary: {
      greeting: 'Bom dia, Aurel!',
      weatherSummary: 'Dia ensolarado.',
      suggestions: 'Ótimo dia.',
      priorityTask: null,
      actionChips: [],
    },
    generatedAt: '2026-05-10T06:00:00Z',
  };

  beforeEach(async () => {
    briefingService = {
      getBriefingByDate: jest.fn().mockReturnValue(of(mockBriefing)),
    };

    await TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: {
                get: (key: string) => key === 'date' ? '2026-05-10' : null,
              },
            },
          },
        },
        { provide: BriefingService, useValue: briefingService },
      ],
    }).compileComponents();

    router = TestBed.inject(Router);
    jest.spyOn(router, 'navigate').mockResolvedValue(true);

    component = TestBed.runInInjectionContext(() => new HistoryDetailComponent());
    component.ngOnInit();
  });

  it('deve ser criado', () => {
    expect(component).toBeTruthy();
  });

  it('deve chamar getBriefingByDate com a data da rota', () => {
    expect(briefingService.getBriefingByDate).toHaveBeenCalledWith('2026-05-10');
  });

  it('deve popular briefing após carregar', () => {
    expect(component.briefing()).toEqual(mockBriefing);
    expect(component.isLoading()).toBe(false);
  });

  it('deve definir erro quando getBriefingByDate falhar', () => {
    (briefingService.getBriefingByDate as jest.Mock).mockReturnValue(
      throwError(() => new Error('Not found'))
    );
    component.ngOnInit();
    expect(component.error()).toBe('Briefing não encontrado para esta data.');
    expect(component.isLoading()).toBe(false);
  });

  it('deve formatar data corretamente', () => {
    const result = component.formatDate('2026-05-10');
    expect(result).toContain('10');
    expect(result).toContain('Maio');
  });
});