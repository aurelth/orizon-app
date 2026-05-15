import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { OnboardingComponent } from './onboarding';
import { UserService } from '../../core/user/services/user.service';
import { ApiService } from '../../core/http/api.service';
import { of, throwError } from 'rxjs';

describe('OnboardingComponent', () => {
  let component: OnboardingComponent;
  let userService: jest.Mocked<Partial<UserService>>;
  let apiService: jest.Mocked<Partial<ApiService>>;
  let router: Router;

  const mockProfile = {
    id: 'user-1',
    email: 'aurel@orizonapp.io',
    displayName: 'Aurel',
    profilePictureUrl: null,
    locationName: 'Blumenau',
    latitude: -26.9194,
    longitude: -49.0661,
    timezone: 'America/Sao_Paulo',
    isTraveling: false,
    travelLocationName: null,
    themePreference: 'Dark' as const,
    googleConnected: false,
    trelloEnabled: false,
    hasCompletedOnboarding: false,
  };

  beforeEach(async () => {
    userService = {
      getProfile: jest.fn().mockReturnValue(of(mockProfile)),
    };

    apiService = {
      post: jest.fn().mockReturnValue(of(null)),
    };

    await TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: UserService, useValue: userService },
        { provide: ApiService, useValue: apiService },
      ],
    }).compileComponents();

    router = TestBed.inject(Router);
    jest.spyOn(router, 'navigate').mockResolvedValue(true);

    component = TestBed.runInInjectionContext(() => new OnboardingComponent());
    component.ngOnInit();
  });

  it('deve ser criado', () => {
    expect(component).toBeTruthy();
  });

  it('deve inicializar no step 1', () => {
    expect(component.currentStep()).toBe(1);
  });

  it('deve ter 5 steps', () => {
    expect(component.totalSteps).toBe(5);
  });

  it('deve calcular progressPercent corretamente no step 1', () => {
    expect(component.progressPercent()).toBe(0);
  });

  it('deve calcular progressPercent corretamente no step 3', () => {
    component.currentStep.set(3);
    expect(component.progressPercent()).toBe(50);
  });

  it('deve calcular progressPercent corretamente no step 5', () => {
    component.currentStep.set(5);
    expect(component.progressPercent()).toBe(100);
  });

  it('isFirstStep deve ser true no step 1', () => {
    expect(component.isFirstStep()).toBe(true);
  });

  it('isLastStep deve ser true no step 5', () => {
    component.currentStep.set(5);
    expect(component.isLastStep()).toBe(true);
  });

  it('next deve avançar para o próximo step', () => {
    component.next();
    expect(component.currentStep()).toBe(2);
  });

  it('next não deve passar do último step', () => {
    component.currentStep.set(5);
    component.next();
    expect(component.currentStep()).toBe(5);
  });

  it('back deve voltar para o step anterior', () => {
    component.currentStep.set(3);
    component.back();
    expect(component.currentStep()).toBe(2);
  });

  it('back não deve voltar antes do step 1', () => {
    component.back();
    expect(component.currentStep()).toBe(1);
  });

  it('goToStep deve ir para o step correto', () => {
    component.goToStep(4);
    expect(component.currentStep()).toBe(4);
  });

  it('goToStep deve ignorar steps inválidos', () => {
    component.goToStep(0);
    expect(component.currentStep()).toBe(1);
    component.goToStep(6);
    expect(component.currentStep()).toBe(1);
  });

  it('deve redirecionar para dashboard se onboarding já concluído', () => {
    (userService.getProfile as jest.Mock).mockReturnValue(
      of({ ...mockProfile, hasCompletedOnboarding: true })
    );
    component.ngOnInit();
    expect(router.navigate).toHaveBeenCalledWith(['/dashboard']);
  });

  it('completeOnboarding deve chamar API e redirecionar', () => {
    component.completeOnboarding();
    expect(apiService.post).toHaveBeenCalledWith(
      '/users/onboarding/complete', {});
    expect(router.navigate).toHaveBeenCalledWith(['/dashboard']);
  });

  it('completeOnboarding deve redirecionar mesmo com erro', () => {
    (apiService.post as jest.Mock).mockReturnValue(
      throwError(() => new Error('error'))
    );
    component.completeOnboarding();
    expect(router.navigate).toHaveBeenCalledWith(['/dashboard']);
  });
});