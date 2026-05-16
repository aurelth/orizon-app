import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { IntegrationsComponent } from './integrations';
import { IntegrationsStore } from '../../../core/integrations/store/integrations.store';
import { GoogleIntegrationService } from '../../../core/integrations/services/google-integration.service';
import { TrelloIntegrationService } from '../../../core/integrations/services/trello-integration.service';
import { BriefingService } from '../../../core/briefing/services/briefing.service';
import { ToastService } from '../../../core/toast/toast.service';
import { of, throwError } from 'rxjs';

describe('IntegrationsComponent', () => {
  let component: IntegrationsComponent;
  let googleService: jest.Mocked<Partial<GoogleIntegrationService>>;
  let trelloService: jest.Mocked<Partial<TrelloIntegrationService>>;
  let briefingService: jest.Mocked<Partial<BriefingService>>;  
  let toastService: jest.Mocked<Partial<ToastService>>;
  let store: InstanceType<typeof IntegrationsStore>;

  const mockBoard = {
    boardId: 'board-1',
    name: 'Orizon',
    color: '#fff',
    lists: [
      { listId: 'list-1', name: 'Today', detectedType: 'today' },
      { listId: 'list-2', name: 'In Progress', detectedType: 'inprogress' },
    ],
  };

  beforeEach(async () => {
    googleService = {
      redirectToGoogle: jest.fn(),
      getStatus: jest.fn().mockReturnValue(of({ connected: false })),      
    };

    trelloService = {
      connect: jest.fn(),
      getBoards: jest.fn().mockReturnValue(of([])),
      saveBoardConfig: jest.fn(),
      getStatus: jest.fn().mockReturnValue(of({ connected: false })),
      getConfig: jest.fn().mockReturnValue(of([])),
      removeBoardConfig: jest.fn(),      
      disconnect: jest.fn().mockReturnValue(of(void 0)),
    };

    briefingService = {
      generateBriefing: jest.fn().mockReturnValue(of({ jobId: '1', message: 'ok' })),
    };
    
    toastService = {
      success: jest.fn(),
      error: jest.fn(),
      info: jest.fn(),
      warning: jest.fn(),
    };

    await TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        IntegrationsStore,
        { provide: GoogleIntegrationService, useValue: googleService },
        { provide: TrelloIntegrationService, useValue: trelloService },
        { provide: BriefingService, useValue: briefingService },        
        { provide: ToastService, useValue: toastService },
      ],
    }).compileComponents();

    store = TestBed.inject(IntegrationsStore);
    component = TestBed.runInInjectionContext(() => new IntegrationsComponent());
    component.ngOnInit();
  });

  it('deve ser criado', () => {
    expect(component).toBeTruthy();
  });

  it('deve inicializar o trelloForm com campos vazios', () => {
    expect(component.trelloForm.get('apiKey')?.value).toBe('');
    expect(component.trelloForm.get('token')?.value).toBe('');
  });

  it('deve inicializar showTrelloForm como false', () => {
    expect(component.showTrelloForm).toBe(false);
  });

  it('deve chamar getStatus do Google no ngOnInit', () => {
    expect(googleService.getStatus).toHaveBeenCalled();
  });

  it('deve chamar getStatus do Trello no ngOnInit', () => {
    expect(trelloService.getStatus).toHaveBeenCalled();
  });

  it('deve alternar showTrelloForm ao chamar toggleTrelloForm', () => {
    component.toggleTrelloForm();
    expect(component.showTrelloForm).toBe(true);
    component.toggleTrelloForm();
    expect(component.showTrelloForm).toBe(false);
  });

  it('deve chamar redirectToGoogle ao conectar Google', () => {
    component.connectGoogle();
    expect(googleService.redirectToGoogle).toHaveBeenCalled();
  });

  it('não deve chamar connect quando trelloForm inválido', () => {
    component.onTrelloSubmit();
    expect(trelloService.connect).not.toHaveBeenCalled();
  });

  it('deve chamar connect com credenciais corretas', () => {
    (trelloService.connect as jest.Mock).mockReturnValue(of(void 0));
    (trelloService.getBoards as jest.Mock).mockReturnValue(of([]));

    component.trelloForm.get('apiKey')?.setValue('a'.repeat(32));
    component.trelloForm.get('token')?.setValue('b'.repeat(64));
    component.onTrelloSubmit();

    expect(trelloService.connect).toHaveBeenCalledWith('a'.repeat(32), 'b'.repeat(64));
  });

  it('deve fechar form e buscar boards após conectar Trello', () => {
    (trelloService.connect as jest.Mock).mockReturnValue(of(void 0));
    (trelloService.getBoards as jest.Mock).mockReturnValue(of([]));

    component.showTrelloForm = true;
    component.trelloForm.get('apiKey')?.setValue('a'.repeat(32));
    component.trelloForm.get('token')?.setValue('b'.repeat(64));
    component.onTrelloSubmit();

    expect(component.showTrelloForm).toBe(false);
    expect(trelloService.getBoards).toHaveBeenCalled();
  });

  it('deve chamar toast.success ao conectar Trello com sucesso', () => {
    (trelloService.connect as jest.Mock).mockReturnValue(of(void 0));
    (trelloService.getBoards as jest.Mock).mockReturnValue(of([]));

    component.trelloForm.get('apiKey')?.setValue('a'.repeat(32));
    component.trelloForm.get('token')?.setValue('b'.repeat(64));
    component.onTrelloSubmit();

    expect(toastService.success).toHaveBeenCalledWith('Trello conectado com sucesso.');
  });
  
  it('deve chamar toast.error quando connect Trello falhar', () => {
    (trelloService.connect as jest.Mock).mockReturnValue(
      throwError(() => new Error('error'))
    );

    component.trelloForm.get('apiKey')?.setValue('a'.repeat(32));
    component.trelloForm.get('token')?.setValue('b'.repeat(64));
    component.onTrelloSubmit();

    expect(toastService.error).toHaveBeenCalledWith('Credenciais Trello inválidas.');
  });

  it('deve retornar true em isBoardActive quando board está nos activeBoardIds', () => {
    store.setActiveBoardIds(['board-1', 'board-2']);
    expect(component.isBoardActive('board-1')).toBe(true);
    expect(component.isBoardActive('board-3')).toBe(false);
  });

  it('deve expandir board ao chamar onBoardExpand', () => {
    component.onBoardExpand(mockBoard);
    expect(component.expandedBoard).toEqual(mockBoard);
    expect(component.selectedTodayList).toBeNull();
    expect(component.selectedInProgressList).toBeNull();
  });

  it('deve colapsar board ao chamar onBoardExpand no mesmo board', () => {
    component.onBoardExpand(mockBoard);
    component.onBoardExpand(mockBoard);
    expect(component.expandedBoard).toBeNull();
  });

  it('deve selecionar lista Today corretamente', () => {
    component.onTodayListSelect(mockBoard.lists[0]);
    expect(component.selectedTodayList).toEqual(mockBoard.lists[0]);
  });

  it('deve selecionar lista InProgress corretamente', () => {
    component.onInProgressListSelect(mockBoard.lists[1]);
    expect(component.selectedInProgressList).toEqual(mockBoard.lists[1]);
  });

  it('deve setar confirmRemoveBoardId ao chamar onRequestRemove', () => {
    component.onRequestRemove('board-1');
    expect(component.confirmRemoveBoardId).toBe('board-1');
  });

  it('deve limpar confirmRemoveBoardId ao chamar onCancelRemove', () => {
    component.confirmRemoveBoardId = 'board-1';
    component.onCancelRemove();
    expect(component.confirmRemoveBoardId).toBeNull();
  });

  it('deve chamar removeBoardConfig e regenerar briefing ao confirmar remoção', () => {
    (trelloService.removeBoardConfig as jest.Mock).mockReturnValue(of(void 0));
    component.confirmRemoveBoardId = 'board-1';
    component.onConfirmRemove();

    expect(trelloService.removeBoardConfig).toHaveBeenCalledWith('board-1');
    expect(briefingService.generateBriefing).toHaveBeenCalled();
  });
  
  it('deve chamar toast.success ao remover board com sucesso', () => {
    (trelloService.removeBoardConfig as jest.Mock).mockReturnValue(of(void 0));
    component.confirmRemoveBoardId = 'board-1';
    component.onConfirmRemove();

    expect(toastService.success).toHaveBeenCalledWith('Board removido do briefing.');
  });

  it('deve limpar confirmRemoveBoardId após confirmar remoção', () => {
    (trelloService.removeBoardConfig as jest.Mock).mockReturnValue(of(void 0));
    component.confirmRemoveBoardId = 'board-1';
    component.onConfirmRemove();

    expect(component.confirmRemoveBoardId).toBeNull();
  });

  it('deve chamar saveBoardConfig e regenerar briefing ao confirmar adição', () => {
    (trelloService.saveBoardConfig as jest.Mock).mockReturnValue(of(void 0));
    component.expandedBoard = mockBoard;
    component.selectedTodayList = mockBoard.lists[0];
    component.selectedInProgressList = mockBoard.lists[1];
    component.onConfirmAdd();

    expect(trelloService.saveBoardConfig).toHaveBeenCalledWith({
      boardId: 'board-1',
      boardName: 'Orizon',
      boardColor: '#fff',
      todayListId: 'list-1',
      todayListName: 'Today',
      inProgressListId: 'list-2',
      inProgressListName: 'In Progress',
    });
    expect(briefingService.generateBriefing).toHaveBeenCalled();
  });
  
  it('deve chamar toast.success ao adicionar board com sucesso', () => {
    (trelloService.saveBoardConfig as jest.Mock).mockReturnValue(of(void 0));
    component.expandedBoard = mockBoard;
    component.selectedTodayList = mockBoard.lists[0];
    component.selectedInProgressList = mockBoard.lists[1];
    component.onConfirmAdd();

    expect(toastService.success).toHaveBeenCalledWith('Board adicionado ao briefing.');
  });

  it('não deve chamar saveBoardConfig quando expandedBoard é null', () => {
    component.expandedBoard = null;
    component.onConfirmAdd();
    expect(trelloService.saveBoardConfig).not.toHaveBeenCalled();
  });

  it('deve validar campo inválido corretamente', () => {
    component.trelloForm.get('apiKey')?.markAsTouched();
    expect(component.isFieldInvalid(component.trelloForm, 'apiKey')).toBe(true);
  });

  it('deve chamar onRequestDisconnectTrello e setar confirmDisconnectTrello', () => {
    component.onRequestDisconnectTrello();
    expect(component.confirmDisconnectTrello).toBe(true);
  });

  it('deve chamar onCancelDisconnectTrello e resetar confirmDisconnectTrello', () => {
    component.confirmDisconnectTrello = true;
    component.onCancelDisconnectTrello();
    expect(component.confirmDisconnectTrello).toBe(false);
  });

  it('deve chamar onConfirmDisconnectTrello e desconectar Trello', () => {
    (trelloService.disconnect as jest.Mock).mockReturnValue(of(void 0));
    component.confirmDisconnectTrello = true;
    component.onConfirmDisconnectTrello();

    expect(trelloService.disconnect).toHaveBeenCalled();
    expect(component.confirmDisconnectTrello).toBe(false);
    expect(component.showTrelloForm).toBe(false);
    expect(component.showBoardSelector).toBe(false);
  });
  
  it('deve chamar toast.info ao desconectar Trello com sucesso', () => {
    (trelloService.disconnect as jest.Mock).mockReturnValue(of(void 0));
    component.onConfirmDisconnectTrello();

    expect(toastService.info).toHaveBeenCalledWith('Trello desconectado.');
  });

  it('deve setar isDisconnectingTrello false quando desconexão falhar', () => {
    (trelloService.disconnect as jest.Mock).mockReturnValue(
      throwError(() => new Error('error'))
    );
    component.onConfirmDisconnectTrello();
    expect(component.isDisconnectingTrello()).toBe(false);
  });
});