import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { AuthService } from '../../../core/auth/services/auth.service';
import { AuthStore } from '../../../core/auth/store/auth.store';
import { RegisterComponent } from './register';
import { of, throwError } from 'rxjs';

describe('RegisterComponent', () => {
  let component: RegisterComponent;
  let authService: jest.Mocked<Partial<AuthService>>;

  const mockAuthResponse = {
    accessToken: 'mock-token',
    refreshToken: 'mock-refresh',
    email: 'aurel@orizonapp.io',
    displayName: 'Aurel',
    expiresIn: 3600,
  };

  beforeEach(async () => {
    authService = {
      register: jest.fn(),
      getAccessToken: jest.fn().mockReturnValue(null),
      getRefreshToken: jest.fn().mockReturnValue(null),
      isAuthenticated: jest.fn().mockReturnValue(false),
      logout: jest.fn(),
    };

    await TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([
          { path: 'dashboard', component: RegisterComponent },
          { path: 'settings', component: RegisterComponent },
        ]),
        AuthStore,
        { provide: AuthService, useValue: authService },
      ],
    }).compileComponents();

    component = TestBed.runInInjectionContext(() => new RegisterComponent());
    component.ngOnInit();
    localStorage.clear();
  });

  afterEach(() => localStorage.clear());

  it('deve ser criado', () => {
    expect(component).toBeTruthy();
  });

  it('deve inicializar o formulário com campos vazios', () => {
    expect(component.form.get('displayName')?.value).toBe('');
    expect(component.form.get('email')?.value).toBe('');
    expect(component.form.get('password')?.value).toBe('');
    expect(component.form.get('confirmPassword')?.value).toBe('');
  });

  it('deve marcar formulário como inválido quando vazio', () => {
    expect(component.form.invalid).toBe(true);
  });

  it('deve validar que as senhas coincidem', () => {
    component.form.get('password')?.setValue('Test@12345');
    component.form.get('confirmPassword')?.setValue('SenhaErrada');
    expect(component.form.errors?.['passwordMismatch']).toBeTruthy();
  });

  it('deve marcar formulário como válido com dados corretos', () => {
    component.form.get('displayName')?.setValue('Aurel');
    component.form.get('email')?.setValue('aurel@orizonapp.io');
    component.form.get('password')?.setValue('Test@12345');
    component.form.get('confirmPassword')?.setValue('Test@12345');
    expect(component.form.valid).toBe(true);
  });

  it('deve alternar visibilidade da senha', () => {
    expect(component.showPassword).toBe(false);
    component.togglePassword();
    expect(component.showPassword).toBe(true);
  });

  it('deve alternar visibilidade da confirmação de senha', () => {
    expect(component.showConfirmPassword).toBe(false);
    component.toggleConfirmPassword();
    expect(component.showConfirmPassword).toBe(true);
  });

  it('não deve chamar register quando formulário inválido', () => {
    component.onSubmit();
    expect(authService.register).not.toHaveBeenCalled();
  });

  it('deve chamar register com dados corretos', () => {
    (authService.register as jest.Mock).mockReturnValue(of(mockAuthResponse));
    component.form.get('displayName')?.setValue('Aurel');
    component.form.get('email')?.setValue('aurel@orizonapp.io');
    component.form.get('password')?.setValue('Test@12345');
    component.form.get('confirmPassword')?.setValue('Test@12345');
    component.onSubmit();
    expect(authService.register).toHaveBeenCalledWith({
      displayName: 'Aurel',
      email: 'aurel@orizonapp.io',
      password: 'Test@12345',
    });
  });

  it('deve definir erro quando registro falhar', () => {
    (authService.register as jest.Mock).mockReturnValue(
      throwError(() => ({ error: { message: 'Email já cadastrado' } }))
    );
    component.form.get('displayName')?.setValue('Aurel');
    component.form.get('email')?.setValue('aurel@orizonapp.io');
    component.form.get('password')?.setValue('Test@12345');
    component.form.get('confirmPassword')?.setValue('Test@12345');
    component.onSubmit();
    expect(component.error()).toBe('Email já cadastrado');
  });
});