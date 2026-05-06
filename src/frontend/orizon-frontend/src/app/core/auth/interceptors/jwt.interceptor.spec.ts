import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptors, HttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { jwtInterceptor } from './jwt.interceptor';
import { AuthService } from '../services/auth.service';
import { AuthStore } from '../store/auth.store';

describe('jwtInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let authService: jest.Mocked<Partial<AuthService>>;

  beforeEach(() => {
    authService = {
      getAccessToken: jest.fn(),
      getRefreshToken: jest.fn().mockReturnValue(null),
      isAuthenticated: jest.fn().mockReturnValue(false),
      logout: jest.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([jwtInterceptor])),
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

  it('deve adicionar header Authorization quando token existe', () => {
    (authService.getAccessToken as jest.Mock).mockReturnValue('mock-token');

    http.get('/test').subscribe();

    const req = httpMock.expectOne('/test');
    expect(req.request.headers.get('Authorization')).toBe('Bearer mock-token');
    req.flush({});
  });

  it('não deve adicionar header Authorization quando token é nulo', () => {
    (authService.getAccessToken as jest.Mock).mockReturnValue(null);

    http.get('/test').subscribe();

    const req = httpMock.expectOne('/test');
    expect(req.request.headers.get('Authorization')).toBeNull();
    req.flush({});
  });
});