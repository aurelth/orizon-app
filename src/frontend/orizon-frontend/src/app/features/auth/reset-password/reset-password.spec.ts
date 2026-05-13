import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter, ActivatedRoute, Router } from '@angular/router';
import { ResetPasswordComponent } from './reset-password';
import { environment } from '../../../../environments/environment';

describe('ResetPasswordComponent', () => {
  let component: ResetPasswordComponent;
  let httpMock: HttpTestingController;
  let router: Router;

  beforeEach(async () => {
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
                get: (key: string) => {
                  if (key === 'email') return 'aurel@orizonapp.io';
                  if (key === 'token') return 'valid-token-123';
                  return null;
                },
              },
            },
          },
        },
      ],
    }).compileComponents();

    component = TestBed.runInInjectionContext(
      () => new ResetPasswordComponent()
    );
    component.ngOnInit();
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    jest.spyOn(router, 'navigate').mockResolvedValue(true);
  });

  afterEach(() => httpMock.verify());

  it('deve ser criado', () => {
    expect(component).toBeTruthy();
  });

  it('deve inicializar isSuccess como false', () => {
    expect(component.isSuccess()).toBe(false);
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

  it('não deve submeter quando form inválido', () => {
    component.form.get('newPassword')?.setValue('');
    component.onSubmit();
    httpMock.expectNone(`${environment.apiUrl}/auth/reset-password`);
  });

  it('deve validar que senhas coincidem', () => {
    component.form.get('newPassword')?.setValue('Test@12345');
    component.form.get('confirmPassword')?.setValue('Diferente@123');
    expect(component.form.errors?.['passwordMismatch']).toBeTruthy();
  });

  it('deve setar isSuccess true após sucesso', () => {
    component.form.get('newPassword')?.setValue('Test@12345');
    component.form.get('confirmPassword')?.setValue('Test@12345');
    component.onSubmit();

    const req = httpMock.expectOne(
      `${environment.apiUrl}/auth/reset-password`
    );
    req.flush({ message: 'Senha redefinida com sucesso.' });

    expect(component.isSuccess()).toBe(true);
    expect(component.isLoading()).toBe(false);
  });

  it('deve setar erro quando token inválido', () => {
    component.form.get('newPassword')?.setValue('Test@12345');
    component.form.get('confirmPassword')?.setValue('Test@12345');
    component.onSubmit();

    const req = httpMock.expectOne(
      `${environment.apiUrl}/auth/reset-password`
    );
    req.flush(
      { message: 'Token inválido ou expirado.' },
      { status: 400, statusText: 'Bad Request' }
    );

    expect(component.error()).toBe('Token inválido ou expirado.');
    expect(component.isLoading()).toBe(false);
  });

  it('deve invalidar senha sem maiúscula', () => {
    component.form.get('newPassword')?.setValue('test@12345');
    component.form.get('newPassword')?.markAsTouched();
    expect(component.form.get('newPassword')?.errors?.['pattern']).toBeTruthy();
  });

  it('deve invalidar senha sem número', () => {
    component.form.get('newPassword')?.setValue('Test@abcde');
    component.form.get('newPassword')?.markAsTouched();
    expect(component.form.get('newPassword')?.errors?.['pattern']).toBeTruthy();
  });
});