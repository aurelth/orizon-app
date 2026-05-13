import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { ForgotPasswordComponent } from './forgot-password';
import { environment } from '../../../../environments/environment';

describe('ForgotPasswordComponent', () => {
  let component: ForgotPasswordComponent;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    }).compileComponents();

    component = TestBed.runInInjectionContext(
      () => new ForgotPasswordComponent()
    );
    component.ngOnInit();
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('deve ser criado', () => {
    expect(component).toBeTruthy();
  });

  it('deve inicializar form com email vazio', () => {
    expect(component.form.get('email')?.value).toBe('');
  });

  it('deve inicializar isSent como false', () => {
    expect(component.isSent()).toBe(false);
  });

  it('não deve submeter quando email inválido', () => {
    component.form.get('email')?.setValue('email-invalido');
    component.onSubmit();
    httpMock.expectNone(`${environment.apiUrl}/auth/forgot-password`);
  });

  it('deve setar isSent true após sucesso', () => {
    component.form.get('email')?.setValue('aurel@orizonapp.io');
    component.onSubmit();

    const req = httpMock.expectOne(
      `${environment.apiUrl}/auth/forgot-password`
    );
    req.flush({ message: 'Email enviado' });

    expect(component.isSent()).toBe(true);
    expect(component.isLoading()).toBe(false);
  });

  it('deve setar erro quando requisição falhar', () => {
    component.form.get('email')?.setValue('aurel@orizonapp.io');
    component.onSubmit();

    const req = httpMock.expectOne(
      `${environment.apiUrl}/auth/forgot-password`
    );
    req.flush({ message: 'Erro' }, { status: 500, statusText: 'Error' });

    expect(component.error()).toBe('Ocorreu um erro. Tente novamente.');
    expect(component.isLoading()).toBe(false);
  });

  it('deve validar email obrigatório', () => {
    component.form.get('email')?.markAsTouched();
    expect(component.isFieldInvalid('email')).toBe(true);
  });
});