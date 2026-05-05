import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { IntegrationsComponent } from './integrations';
import { IntegrationsStore } from '../../../core/integrations/store/integrations.store';
import { GoogleIntegrationService } from '../../../core/integrations/services/google-integration.service';
import { TrelloIntegrationService } from '../../../core/integrations/services/trello-integration.service';
import { of, throwError } from 'rxjs';

describe('IntegrationsComponent', () => {
  let component: IntegrationsComponent;
  let googleService: jest.Mocked<Partial<GoogleIntegrationService>>;
  let trelloService: jest.Mocked<Partial<TrelloIntegrationService>>;
  let store: InstanceType<typeof IntegrationsStore>;

  beforeEach(async () => {
    googleService = {
      redirectToGoogle: jest.fn(),
      getAuthUrl: jest.fn(),
    };

    trelloService = {
      connect: jest.fn(),
      getBoards: jest.fn(),
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
    const board = { id: 'board-1', name: 'Orizon' };
    component.onBoardSelect(board);
    expect(component.selectedBoard).toEqual(board);
    expect(component.boardForm.get('boardId')?.value).toBe('board-1');
  });

  it('deve chamar saveBoardConfig com board selecionado', () => {
    (trelloService.saveBoardConfig as jest.Mock).mockReturnValue(of(void 0));
    component.selectedBoard = { id: 'board-1', name: 'Orizon' };
    component.boardForm.get('boardId')?.setValue('board-1');
    component.onSaveBoardConfig();
    expect(trelloService.saveBoardConfig).toHaveBeenCalledWith({
      boardId: 'board-1',
      listIds: [],
    });
  });

  it('deve validar campo inválido corretamente', () => {
    component.trelloForm.get('apiKey')?.markAsTouched();
    expect(component.isFieldInvalid(component.trelloForm, 'apiKey')).toBe(true);
  });
});