import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, ActivatedRoute, Router } from '@angular/router';
import { OAuthCallbackComponent } from './oauth-callback';
import { ApiService } from '../../../core/http/api.service';
import { of, throwError } from 'rxjs';

describe('OAuthCallbackComponent', () => {
  let component: OAuthCallbackComponent;
  let apiService: jest.Mocked<Partial<ApiService>>;
  let router: Router;
  let queryParams: Record<string, string> = {};

  beforeEach(async () => {    
    queryParams = {};

    apiService = {
      post: jest.fn(),
    };

    await TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ApiService, useValue: apiService },        
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParamMap: {
                get: (key: string) => queryParams[key] ?? null,
              },
            },
          },
        },
      ],
    }).compileComponents();

    router = TestBed.inject(Router);
    jest.spyOn(router, 'navigate').mockResolvedValue(true);
  });

  it('deve ser criado', () => {
    queryParams = { code: 'auth-code', state: 'state-123' };
    component = TestBed.runInInjectionContext(() => new OAuthCallbackComponent());
    expect(component).toBeTruthy();
  });

  it('deve definir erro quando parâmetro error está presente', () => {
    queryParams = { error: 'access_denied' };
    component = TestBed.runInInjectionContext(() => new OAuthCallbackComponent());
    component.ngOnInit();
    expect(component.error).toBe('Autorização negada pelo Google.');
  });

  it('deve definir erro quando code está ausente', () => {
    queryParams = {};
    component = TestBed.runInInjectionContext(() => new OAuthCallbackComponent());
    component.ngOnInit();
    expect(component.error).toBe('Código de autorização não encontrado.');
  });

  it('deve chamar POST /google/callback com code e state', () => {
    (apiService.post as jest.Mock).mockReturnValue(of(void 0));
    queryParams = { code: 'auth-code-123', state: 'state-xyz' };
    component = TestBed.runInInjectionContext(() => new OAuthCallbackComponent());
    component.ngOnInit();
    expect(apiService.post).toHaveBeenCalledWith('/google/callback', {
      code: 'auth-code-123',
      state: 'state-xyz',
    });
  });

  it('deve navegar para settings/integrations após callback bem-sucedido', () => {
    (apiService.post as jest.Mock).mockReturnValue(of(void 0));
    queryParams = { code: 'auth-code-123', state: 'state-xyz' };
    component = TestBed.runInInjectionContext(() => new OAuthCallbackComponent());
    component.ngOnInit();
    expect(router.navigate).toHaveBeenCalledWith(['/settings/integrations']);
  });

  it('deve definir erro quando POST /google/callback falhar', () => {
    (apiService.post as jest.Mock).mockReturnValue(
      throwError(() => new Error('Server error'))
    );
    queryParams = { code: 'auth-code-123', state: 'state-xyz' };
    component = TestBed.runInInjectionContext(() => new OAuthCallbackComponent());
    component.ngOnInit();
    expect(component.error).toBe('Falha ao conectar com o Google. Tente novamente.');
  });

  it('deve navegar para settings ao chamar goToSettings', () => {
    queryParams = { code: 'auth-code' };
    component = TestBed.runInInjectionContext(() => new OAuthCallbackComponent());
    component.goToSettings();
    expect(router.navigate).toHaveBeenCalledWith(['/settings/integrations']);
  });
});