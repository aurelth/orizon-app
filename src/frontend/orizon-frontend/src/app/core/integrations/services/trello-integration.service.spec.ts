import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { TrelloIntegrationService } from './trello-integration.service';
import { IntegrationsStore } from '../store/integrations.store';
import { ApiService } from '../../http/api.service';
import { of, throwError } from 'rxjs';

describe('TrelloIntegrationService', () => {
  let service: TrelloIntegrationService;
  let apiService: jest.Mocked<Partial<ApiService>>;
  let store: InstanceType<typeof IntegrationsStore>;

  const mockBoards = [
    { boardId: 'board-1', name: 'Projeto Orizon', lists: [], color: '#fff' },
    { boardId: 'board-2', name: 'Backlog', lists: [], color: '#000' },
  ];

  beforeEach(() => {
    apiService = {
      post: jest.fn(),
      get: jest.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        IntegrationsStore,
        { provide: ApiService, useValue: apiService },
      ],
    });

    service = TestBed.inject(TrelloIntegrationService);
    store = TestBed.inject(IntegrationsStore);
  });

  it('deve ser criado', () => {
    expect(service).toBeTruthy();
  });

  it('deve marcar trelloConnected como true após conectar com sucesso', () => {
    (apiService.post as jest.Mock).mockReturnValue(of(void 0));

    service.connect(
      'mock-api-key-32chars-xxxxxxxxxxx',
      'mock-token-64chars-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx'
    ).subscribe();

    expect(store.trelloConnected()).toBe(true);
  });

  it('deve definir erro no store quando connect falhar', () => {
    (apiService.post as jest.Mock).mockReturnValue(
      throwError(() => new Error('Unauthorized'))
    );

    service.connect('invalid-key', 'invalid-token').subscribe({ error: () => {} });

    expect(store.error()).toBe('Credenciais Trello inválidas.');
  });

  it('deve retornar e armazenar boards no store', () => {
    (apiService.get as jest.Mock).mockReturnValue(of(mockBoards));

    service.getBoards('apikey', 'token').subscribe((boards) => {
      expect(boards).toHaveLength(2);
      expect(boards[0].name).toBe('Projeto Orizon');
    });

    expect(store.trelloBoards()).toEqual(mockBoards);
  });

  it('deve definir erro no store quando getBoards falhar', () => {
    (apiService.get as jest.Mock).mockReturnValue(
      throwError(() => new Error('Forbidden'))
    );

    service.getBoards('apikey', 'token').subscribe({ error: () => {} });

    expect(store.error()).toBe('Falha ao carregar boards do Trello.');
  });

  it('deve salvar configuração de board e atualizar store', () => {
    (apiService.post as jest.Mock).mockReturnValue(of(void 0));

    service.saveBoardConfig({
      boardId: 'board-1',
      boardName: 'Projeto Orizon',
      todayListId: 'list-1',
      inProgressListId: 'list-2',
    }).subscribe();

    expect(store.trelloBoardConfig()).toMatchObject({
      boardId: 'board-1',
      listIds: ['list-1', 'list-2'],
    });
  });

  it('deve definir erro quando saveBoardConfig falhar', () => {
    (apiService.post as jest.Mock).mockReturnValue(
      throwError(() => new Error('Bad Request'))
    );

    service.saveBoardConfig({
      boardId: 'board-1',
      boardName: 'Test',
    }).subscribe({ error: () => {} });

    expect(store.error()).toBe('Falha ao salvar configuração do board.');
  });
});