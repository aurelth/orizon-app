import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { HistoryComponent } from './history';
import {
  BriefingService,
  BriefingHistoryResult,
  UserStats,
} from '../../core/briefing/services/briefing.service';
import { of, throwError } from 'rxjs';

describe('HistoryComponent', () => {
  let component: HistoryComponent;
  let briefingService: jest.Mocked<Partial<BriefingService>>;
  let router: Router;

  const mockHistory: BriefingHistoryResult = {
    items: [
      {
        briefingId: 'b1',
        date: '2026-05-10',
        status: 'Generated',
        greeting: 'Bom dia, Aurel!',
        weatherEmoji: '☀️',
        generatedAt: '2026-05-10T06:00:00Z',
      },
      {
        briefingId: 'b2',
        date: '2026-05-09',
        status: 'Generated',
        greeting: 'Boa tarde, Aurel!',
        weatherEmoji: '⛅',
        generatedAt: '2026-05-09T12:00:00Z',
      },
    ],
    page: 1,
    pageSize: 10,
    total: 2,
    totalPages: 1,
  };

  const mockStats: UserStats = {
    totalGenerated: 10,
    currentStreak: 3,
    maxStreak: 7,
  };

  beforeEach(async () => {
    briefingService = {
      getHistory: jest.fn().mockReturnValue(of(mockHistory)),
      getStats: jest.fn().mockReturnValue(of(mockStats)),
    };

    await TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: BriefingService, useValue: briefingService },
      ],
    }).compileComponents();

    router = TestBed.inject(Router);
    jest.spyOn(router, 'navigate').mockResolvedValue(true);

    component = TestBed.runInInjectionContext(() => new HistoryComponent());
    component.ngOnInit();
  });

  it('deve ser criado', () => {
    expect(component).toBeTruthy();
  });

  it('deve chamar getHistory e getStats no ngOnInit', () => {
    expect(briefingService.getHistory).toHaveBeenCalledWith(1, 10, undefined, undefined);
    expect(briefingService.getStats).toHaveBeenCalled();
  });

  it('deve popular history e stats após carregar', () => {
    expect(component.history()).toEqual(mockHistory);
    expect(component.stats()).toEqual(mockStats);
    expect(component.isLoading()).toBe(false);
    expect(component.isLoadingStats()).toBe(false);
  });

  it('deve definir erro quando getHistory falhar', () => {
    (briefingService.getHistory as jest.Mock).mockReturnValue(
      throwError(() => new Error('Network error'))
    );
    component.loadHistory();
    expect(component.error()).toBe('Falha ao carregar histórico.');
    expect(component.isLoading()).toBe(false);
  });

  it('deve navegar para detalhe ao chamar openBriefing', () => {
    component.openBriefing('2026-05-10');
    expect(router.navigate).toHaveBeenCalledWith(['/history', '2026-05-10']);
  });

  it('deve carregar página correta ao chamar loadHistory com page', () => {
    component.loadHistory(2);
    expect(briefingService.getHistory).toHaveBeenCalledWith(2, 10, undefined, undefined);
    expect(component.currentPage).toBe(2);
  });

  it('deve aplicar filtro de semana ao chamar setPeriodFilter week', () => {
    component.setPeriodFilter('week');
    expect(component.activePeriod).toBe('week');
    expect(briefingService.getHistory).toHaveBeenCalledWith(
      1, 10, expect.any(String), undefined
    );
  });

  it('deve aplicar filtro de mês ao chamar setPeriodFilter month', () => {
    component.setPeriodFilter('month');
    expect(component.activePeriod).toBe('month');
    expect(briefingService.getHistory).toHaveBeenCalledWith(
      1, 10, expect.any(String), undefined
    );
  });

  it('deve remover filtros ao chamar setPeriodFilter all', () => {
    component.setPeriodFilter('month');
    component.setPeriodFilter('all');
    expect(component.activePeriod).toBe('all');
    expect(briefingService.getHistory).toHaveBeenLastCalledWith(1, 10, undefined, undefined);
  });

  it('deve retornar label correto para status', () => {
    expect(component.getStatusLabel('Generated')).toBe('Gerado');
    expect(component.getStatusLabel('Failed')).toBe('Falhou');
    expect(component.getStatusLabel('Pending')).toBe('Pendente');
  });

  it('deve formatar data corretamente', () => {
    const result = component.formatDate('2026-05-10');
    expect(result).toContain('10');
    expect(result).toContain('maio');
  });
});