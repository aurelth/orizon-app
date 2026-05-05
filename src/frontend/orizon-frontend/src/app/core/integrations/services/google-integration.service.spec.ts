import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { GoogleIntegrationService } from './google-integration.service';
import { IntegrationsStore } from '../store/integrations.store';
import { ApiService } from '../../http/api.service';
import { of, throwError } from 'rxjs';

describe('GoogleIntegrationService', () => {
  let service: GoogleIntegrationService;
  let apiService: jest.Mocked<Partial<ApiService>>;
  let store: InstanceType<typeof IntegrationsStore>;

  beforeEach(() => {
    apiService = {
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

    service = TestBed.inject(GoogleIntegrationService);
    store = TestBed.inject(IntegrationsStore);
  });

  it('deve ser criado', () => {
    expect(service).toBeTruthy();
  });
  
  it('deve ativar isLoadingGoogle antes de chamar getAuthUrl', () => {
    (apiService.get as jest.Mock).mockReturnValue(of({ url: 'https://accounts.google.com/auth' }));

    store.setLoadingGoogle(false);
    service.getAuthUrl().subscribe();

    expect(store.isLoadingGoogle()).toBe(true);
  });

  it('deve retornar url ao chamar getAuthUrl com sucesso', () => {
    const mockUrl = 'https://accounts.google.com/auth?client_id=test';
    (apiService.get as jest.Mock).mockReturnValue(of({ url: mockUrl }));

    service.getAuthUrl().subscribe((result) => {
      expect(result.url).toBe(mockUrl);
    });
  });

  it('deve definir erro no store quando getAuthUrl falhar', () => {
    (apiService.get as jest.Mock).mockReturnValue(
      throwError(() => new Error('Falha na requisição'))
    );

    service.getAuthUrl().subscribe({ error: () => {} });
    expect(store.error()).toBe('Falha ao obter URL de autenticação Google.');
  });
});