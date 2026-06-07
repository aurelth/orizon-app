import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { DashboardComponent } from './dashboard';
import { BriefingService, UserStats } from '../../core/briefing/services/briefing.service';
import { BriefingStore } from '../../core/briefing/store/briefing.store';
import { ToastService } from '../../core/toast/toast.service';
import { of, throwError } from 'rxjs';

describe('DashboardComponent', () => {
  let component: DashboardComponent;
  let briefingService: jest.Mocked<Partial<BriefingService>>;
  let toastService: jest.Mocked<Partial<ToastService>>;

  const mockStats: UserStats = {
    totalGenerated: 42,
    currentStreak: 7,
    maxStreak: 15,
  };

  const mockBriefing = {
    briefingId: '1',
    date: '2026-06-06',
    status: 'Generated',
    generatedAt: '2026-06-06T06:00:00Z',
    aiSummary: {
      greeting: 'Bom dia, Aurel!',
      weatherSummary: 'Dia ensolarado.',
      suggestions: 'Ótimo dia!',
      actionChips: [],
    },
    weather: {
      currentTemperature: 22,
      description: 'Ensolarado',
      weatherEmoji: '☀️',
      locationName: 'Blumenau',
    },
    emails: [],
    calendarEvents: [],
    trelloTasks: null,
    googleTasks: null,
  };

  beforeEach(async () => {
    briefingService = {
      getTodayBriefing: jest.fn().mockReturnValue(of(mockBriefing)),
      connectSignalR: jest.fn(),
      getStats: jest.fn().mockReturnValue(of(mockStats)),
      generateBriefing: jest.fn(),
    };

    toastService = {
      success: jest.fn(),
      error: jest.fn(),
      info: jest.fn(),
    };

    await TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        BriefingStore,
        { provide: BriefingService, useValue: briefingService },
        { provide: ToastService, useValue: toastService },
      ],
    }).compileComponents();

    component = TestBed.runInInjectionContext(() => new DashboardComponent());
    component.ngOnInit();
  });

  it('deve ser criado', () => {
    expect(component).toBeTruthy();
  });

  it('deve chamar getTodayBriefing no ngOnInit', () => {
    expect(briefingService.getTodayBriefing).toHaveBeenCalled();
  });

  it('deve chamar connectSignalR no ngOnInit', () => {
    expect(briefingService.connectSignalR).toHaveBeenCalled();
  });

  it('deve chamar getStats no ngOnInit', () => {
    expect(briefingService.getStats).toHaveBeenCalled();
  });

  it('deve carregar stats corretamente', () => {
    expect(component.stats()).toEqual(mockStats);
  });

  it('deve definir isLoadingStats como false após carregar', () => {
    expect(component.isLoadingStats()).toBe(false);
  });

  it('deve definir isLoadingStats como false quando getStats falhar', () => {
    (briefingService.getStats as jest.Mock).mockReturnValue(throwError(() => new Error('error')));
    component.loadStats();
    expect(component.isLoadingStats()).toBe(false);
  });

  it('streakLabel deve retornar mensagem correta para streak 0', () => {
    expect(component.streakLabel(0)).toBe('Nenhum dia consecutivo');
  });

  it('streakLabel deve retornar mensagem correta para streak 1', () => {
    expect(component.streakLabel(1)).toBe('1 dia consecutivo');
  });

  it('streakLabel deve retornar mensagem correta para streak maior que 1', () => {
    expect(component.streakLabel(7)).toBe('7 dias consecutivos');
  });

  it('deve formatar data corretamente', () => {
    const result = component.formatDate('2026-06-06');
    expect(result).toContain('Junho');
  });

  it('deve iniciar isGenerating como false', () => {
    expect(component.isGenerating()).toBe(false);
  });

  it('deve iniciar generateError como null', () => {
    expect(component.generateError()).toBeNull();
  });

  it('deve chamar generateBriefing e mostrar toast.info no sucesso', () => {
    (briefingService.generateBriefing as jest.Mock).mockReturnValue(
      of({ jobId: '1', message: 'ok' }),
    );
    component.generateBriefing();
    expect(briefingService.generateBriefing).toHaveBeenCalled();
    expect(toastService.info).toHaveBeenCalledWith(
      'Briefing sendo gerado. Aguarde alguns instantes.',
    );
  });

  it('deve chamar toast.error e definir generateError quando generateBriefing falhar', () => {
    const errorMsg = 'Erro ao gerar briefing.';
    (briefingService.generateBriefing as jest.Mock).mockReturnValue(
      throwError(() => ({ error: { message: errorMsg } })),
    );
    component.generateBriefing();
    expect(component.generateError()).toBe(errorMsg);
    expect(toastService.error).toHaveBeenCalledWith(errorMsg);
  });

  it('deve definir isGenerating como false após sucesso', () => {
    (briefingService.generateBriefing as jest.Mock).mockReturnValue(
      of({ jobId: '1', message: 'ok' }),
    );
    component.generateBriefing();
    expect(component.isGenerating()).toBe(false);
  });

  it('deve definir isGenerating como false após erro', () => {
    (briefingService.generateBriefing as jest.Mock).mockReturnValue(
      throwError(() => new Error('error')),
    );
    component.generateBriefing();
    expect(component.isGenerating()).toBe(false);
  });

  it('stats deve iniciar como null antes de carregar', () => {
    const freshComponent = TestBed.runInInjectionContext(() => new DashboardComponent());
    expect(freshComponent.stats()).toBeNull();
  });
});
