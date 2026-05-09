import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { IntegrationsComponent } from './integrations';
import { IntegrationsStore } from '../../../core/integrations/store/integrations.store';
import { GoogleIntegrationService } from '../../../core/integrations/services/google-integration.service';
import { TrelloIntegrationService } from '../../../core/integrations/services/trello-integration.service';
import { of } from 'rxjs';

describe('IntegrationsComponent', () => {
  let component: IntegrationsComponent;
  let googleService: jest.Mocked<Partial<GoogleIntegrationService>>;
  let trelloService: jest.Mocked<Partial<TrelloIntegrationService>>;
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
      getStatus: jest.fn().mockReturnValue(of(null)),
    };

    trelloService = {
      connect: jest.fn(),
      getBoards: jest.fn().mockReturnValue(of([])),
      saveBoardConfig: jest.fn(),
    };

    await TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        IntegrationsStore,
        { provide: GoogleIntegrationService, useValue: googleService },
        { provide: TrelloIntegrationService, useValue: trelloService },
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

  it('deve selecionar board corretamente', () => {
    component.onBoardSelect(mockBoard);
    expect(component.selectedBoard).toEqual(mockBoard);
    expect(component.selectedTodayList).toBeNull();
    expect(component.selectedInProgressList).toBeNull();
  });

  it('deve selecionar lista Today corretamente', () => {
    component.onBoardSelect(mockBoard);
    component.onTodayListSelect(mockBoard.lists[0]);
    expect(component.selectedTodayList).toEqual(mockBoard.lists[0]);
  });

  it('deve selecionar lista InProgress corretamente', () => {
    component.onBoardSelect(mockBoard);
    component.onInProgressListSelect(mockBoard.lists[1]);
    expect(component.selectedInProgressList).toEqual(mockBoard.lists[1]);
  });

  it('deve chamar saveBoardConfig com board e listas selecionadas', () => {
    (trelloService.saveBoardConfig as jest.Mock).mockReturnValue(of(void 0));

    component.selectedBoard = mockBoard;
    component.selectedTodayList = mockBoard.lists[0];
    component.selectedInProgressList = mockBoard.lists[1];
    component.onSaveBoardConfig();

    expect(trelloService.saveBoardConfig).toHaveBeenCalledWith({
      boardId: 'board-1',
      boardName: 'Orizon',
      boardColor: '#fff',
      todayListId: 'list-1',
      todayListName: 'Today',
      inProgressListId: 'list-2',
      inProgressListName: 'In Progress',
    });
  });

  it('deve validar campo inválido corretamente', () => {
    component.trelloForm.get('apiKey')?.markAsTouched();
    expect(component.isFieldInvalid(component.trelloForm, 'apiKey')).toBe(true);
  });
});