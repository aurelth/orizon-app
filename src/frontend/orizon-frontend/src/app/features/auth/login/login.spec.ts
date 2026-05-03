import { TestBed } from '@angular/core/testing';
import { FormBuilder } from '@angular/forms';
import { Router, provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { AuthService } from '../../../core/auth/services/auth.service';
import { AuthStore } from '../../../core/auth/store/auth.store';
import { LoginComponent } from './login';
import { of, throwError } from 'rxjs';

describe('LoginComponent', () => {
  let component: LoginComponent;
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
      login: jest.fn(),
      getAccessToken: jest.fn().mockReturnValue(null),
      getRefreshToken: jest.fn().mockReturnValue(null),
      isAuthenticated: jest.fn().mockReturnValue(false),
      logout: jest.fn(),
    };

    await TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: 'dashboard', component: LoginComponent }]),
        AuthStore,
        { provide: AuthService, useValue: authService },
      ],
    }).compileComponents();

    component = TestBed.runInInjectionContext(() => new LoginComponent());
    component.ngOnInit();
    localStorage.clear();
  });

  afterEach(() => localStorage.clear());

  it('deve ser criado', () => {
    expect(component).toBeTruthy();
  });

  it('deve inicializar o formulário com campos vazios', () => {
    expect(component.form.get('email')?.value).toBe('');
    expect(component.form.get('password')?.value).toBe('');
  });

  it('deve marcar formulário como inválido quando vazio', () => {
    expect(component.form.invalid).toBe(true);
  });

  it('deve marcar email como inválido quando formato incorreto', () => {
    component.form.get('email')?.setValue('email-invalido');
    component.form.get('email')?.markAsTouched();
    expect(component.isFieldInvalid('email')).toBe(true);
  });

  it('deve marcar formulário como válido com dados corretos', () => {
    component.form.get('email')?.setValue('aurel@orizonapp.io');
    component.form.get('password')?.setValue('Test@12345');
    expect(component.form.valid).toBe(true);
  });

  it('deve alternar visibilidade da senha', () => {
    expect(component.showPassword).toBe(false);
    component.togglePassword();
    expect(component.showPassword).toBe(true);
    component.togglePassword();
    expect(component.showPassword).toBe(false);
  });

  it('não deve chamar login quando formulário inválido', () => {
    component.onSubmit();
    expect(authService.login).not.toHaveBeenCalled();
  });

  it('deve chamar login com dados corretos', () => {
    (authService.login as jest.Mock).mockReturnValue(of(mockAuthResponse));
    component.form.get('email')?.setValue('aurel@orizonapp.io');
    component.form.get('password')?.setValue('Test@12345');
    component.onSubmit();
    expect(authService.login).toHaveBeenCalledWith({
      email: 'aurel@orizonapp.io',
      password: 'Test@12345',
    });
  });

  it('deve definir erro quando login falhar', () => {
    (authService.login as jest.Mock).mockReturnValue(
      throwError(() => ({ error: { message: 'Credenciais inválidas' } }))
    );
    component.form.get('email')?.setValue('aurel@orizonapp.io');
    component.form.get('password')?.setValue('Test@12345');
    component.onSubmit();
    expect(component.error()).toBe('Credenciais inválidas');
  });
});