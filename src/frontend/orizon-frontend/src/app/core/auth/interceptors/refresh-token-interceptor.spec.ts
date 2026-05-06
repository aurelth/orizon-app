import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptors, HttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { refreshTokenInterceptor } from './refresh-token.interceptor';
import { AuthService } from '../services/auth.service';
import { AuthStore } from '../store/auth.store';
import { of, throwError } from 'rxjs';

describe('refreshTokenInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let authService: jest.Mocked<Partial<AuthService>>;

  const mockAuthResponse = {
    accessToken: 'new-access-token',
    refreshToken: 'new-refresh-token',
    email: 'aurel@orizonapp.io',
    displayName: 'Aurel',
    expiresIn: 3600,
  };

  beforeEach(() => {
    authService = {
      getAccessToken: jest.fn().mockReturnValue(null),
      getRefreshToken: jest.fn(),
      refresh: jest.fn(),
      logout: jest.fn(),
      isAuthenticated: jest.fn().mockReturnValue(false),
    };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([refreshTokenInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
        AuthStore,
        { provide: AuthService, useValue: authService },
      ],
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('deve deixar requisição passar quando não há erro 401', () => {
    http.get('/test').subscribe();

    const req = httpMock.expectOne('/test');
    req.flush({ data: 'ok' });
  });

  it('deve tentar refresh quando receber 401 com refreshToken disponível', () => {
    (authService.getRefreshToken as jest.Mock).mockReturnValue('valid-refresh-token');
    (authService.refresh as jest.Mock).mockReturnValue(of(mockAuthResponse));

    http.get('/test').subscribe();

    const req = httpMock.expectOne('/test');
    req.flush({}, { status: 401, statusText: 'Unauthorized' });

    // requisição repetida após refresh com novo token
    const retryReq = httpMock.expectOne('/test');
    expect(retryReq.request.headers.get('Authorization')).toBe('Bearer new-access-token');
    retryReq.flush({ data: 'ok' });
  });

  it('deve chamar logout quando não há refreshToken e receber 401', () => {
    (authService.getRefreshToken as jest.Mock).mockReturnValue(null);

    http.get('/test').subscribe({ error: () => {} });

    const req = httpMock.expectOne('/test');
    req.flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(authService.logout).toHaveBeenCalled();
  });

  it('deve chamar logout quando refresh falhar', () => {
    (authService.getRefreshToken as jest.Mock).mockReturnValue('expired-refresh-token');
    (authService.refresh as jest.Mock).mockReturnValue(
      throwError(() => ({ status: 401, message: 'Refresh expirado' }))
    );

    http.get('/test').subscribe({ error: () => {} });

    const req = httpMock.expectOne('/test');
    req.flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(authService.logout).toHaveBeenCalled();
  });

  it('deve propagar erros que não são 401', () => {
    let errorReceived: unknown;

    http.get('/test').subscribe({ error: (err) => (errorReceived = err) });

    const req = httpMock.expectOne('/test');
    req.flush({}, { status: 500, statusText: 'Internal Server Error' });

    expect(authService.logout).not.toHaveBeenCalled();
    expect(authService.refresh).not.toHaveBeenCalled();
  });
});