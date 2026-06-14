import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { DashboardComponent } from './dashboard';
import { BriefingService } from '../../core/briefing/services/briefing.service';
import { BriefingStore } from '../../core/briefing/store/briefing.store';
import { ToastService } from '../../core/toast/toast.service';
import { of, throwError } from 'rxjs';

describe('DashboardComponent', () => {
  let component: DashboardComponent;
  let briefingService: jest.Mocked<Partial<BriefingService>>;
  let toastService: jest.Mocked<Partial<ToastService>>;

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
      locationName: 'Blumenau, SC',
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
      of({ jobId: '1', message: 'ok' })
    );
    component.generateBriefing();
    expect(briefingService.generateBriefing).toHaveBeenCalled();
    expect(toastService.info).toHaveBeenCalledWith(
      'Briefing sendo gerado. Aguarde alguns instantes.'
    );
  });

  it('deve chamar toast.error e definir generateError quando generateBriefing falhar', () => {
    const errorMsg = 'Erro ao gerar briefing.';
    (briefingService.generateBriefing as jest.Mock).mockReturnValue(
      throwError(() => ({ error: { message: errorMsg } }))
    );
    component.generateBriefing();
    expect(component.generateError()).toBe(errorMsg);
    expect(toastService.error).toHaveBeenCalledWith(errorMsg);
  });

  it('deve definir isGenerating como false após sucesso', () => {
    (briefingService.generateBriefing as jest.Mock).mockReturnValue(
      of({ jobId: '1', message: 'ok' })
    );
    component.generateBriefing();
    expect(component.isGenerating()).toBe(false);
  });

  it('deve definir isGenerating como false após erro', () => {
    (briefingService.generateBriefing as jest.Mock).mockReturnValue(
      throwError(() => new Error('error'))
    );
    component.generateBriefing();
    expect(component.isGenerating()).toBe(false);
  });
});