import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, ActivatedRoute, Router } from '@angular/router';
import { OAuthCallbackComponent } from './oauth-callback';

describe('OAuthCallbackComponent', () => {
  let component: OAuthCallbackComponent;
  let router: Router;
  let queryParams: Record<string, string> = {};

  beforeEach(async () => {
    queryParams = {};
    jest.useFakeTimers();

    await TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
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

  afterEach(() => {
    jest.useRealTimers();
  });

  it('deve ser criado', () => {
    queryParams = {};
    component = TestBed.runInInjectionContext(() => new OAuthCallbackComponent());
    expect(component).toBeTruthy();
  });

  it('deve definir erro quando parâmetro error está presente', () => {
    queryParams = { error: 'access_denied' };
    component = TestBed.runInInjectionContext(() => new OAuthCallbackComponent());
    component.ngOnInit();
    expect(component.error).toBe('Autorização negada pelo Google.');
  });

  it('não deve definir erro quando error está ausente', () => {
    queryParams = {};
    component = TestBed.runInInjectionContext(() => new OAuthCallbackComponent());
    component.ngOnInit();
    expect(component.error).toBeNull();
  });

  it('deve navegar para settings/integrations após 1 segundo quando sem erro', () => {
    queryParams = {};
    component = TestBed.runInInjectionContext(() => new OAuthCallbackComponent());
    component.ngOnInit();
    jest.advanceTimersByTime(1000);
    expect(router.navigate).toHaveBeenCalledWith(['/settings/integrations']);
  });

  it('não deve navegar quando há erro', () => {
    queryParams = { error: 'access_denied' };
    component = TestBed.runInInjectionContext(() => new OAuthCallbackComponent());
    component.ngOnInit();
    jest.advanceTimersByTime(1000);
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('deve navegar para settings ao chamar goToSettings', () => {
    queryParams = {};
    component = TestBed.runInInjectionContext(() => new OAuthCallbackComponent());
    component.goToSettings();
    expect(router.navigate).toHaveBeenCalledWith(['/settings/integrations']);
  });
});