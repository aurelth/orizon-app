import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { Component } from '@angular/core';
import { AuthService } from './auth.service';
import { AuthStore } from '../store/auth.store';
import { environment } from '../../../../environments/environment';

@Component({ standalone: true, template: '' })
class DummyComponent {}

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  const mockAuthResponse = {
    accessToken: 'mock-access-token',
    refreshToken: 'mock-refresh-token',
    email: 'aurel@orizonapp.io',
    displayName: 'Aurel',
    expiresIn: 3600,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: 'auth/login', component: DummyComponent }]),
        AuthStore,
        AuthService,
      ],
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
    localStorage.clear();
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('deve ser criado', () => {
    expect(service).toBeTruthy();
  });

  describe('login()', () => {
    it('deve chamar POST /auth/login e retornar AuthResponse', (done) => {
      service.login({ email: 'aurel@orizonapp.io', password: 'Test@12345' }).subscribe((response) => {
        expect(response.accessToken).toBe('mock-access-token');
        expect(response.email).toBe('aurel@orizonapp.io');
        done();
      });
      const req = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
      expect(req.request.method).toBe('POST');
      req.flush(mockAuthResponse);
    });

    it('deve salvar tokens no localStorage após login', (done) => {
      service.login({ email: 'aurel@orizonapp.io', password: 'Test@12345' }).subscribe(() => {
        expect(localStorage.getItem('access_token')).toBe('mock-access-token');
        expect(localStorage.getItem('refresh_token')).toBe('mock-refresh-token');
        done();
      });
      const req = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
      req.flush(mockAuthResponse);
    });
  });

  describe('register()', () => {
    it('deve chamar POST /auth/register e retornar AuthResponse', (done) => {
      service.register({
        displayName: 'Aurel',
        email: 'aurel@orizonapp.io',
        password: 'Test@12345',
      }).subscribe((response) => {
        expect(response.accessToken).toBe('mock-access-token');
        done();
      });
      const req = httpMock.expectOne(`${environment.apiUrl}/auth/register`);
      expect(req.request.method).toBe('POST');
      req.flush(mockAuthResponse);
    });
  });

  describe('refresh()', () => {
    it('deve chamar POST /auth/refresh com o refreshToken', (done) => {
      service.refresh('mock-refresh-token').subscribe((response) => {
        expect(response.accessToken).toBe('mock-access-token');
        done();
      });
      const req = httpMock.expectOne(`${environment.apiUrl}/auth/refresh`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ refreshToken: 'mock-refresh-token' });
      req.flush(mockAuthResponse);
    });
  });

  describe('isAuthenticated()', () => {
    it('deve retornar false quando não há token', () => {
      expect(service.isAuthenticated()).toBe(false);
    });

    it('deve retornar true quando há access token', () => {
      localStorage.setItem('access_token', 'mock-token');
      expect(service.isAuthenticated()).toBe(true);
    });
  });

  describe('getAccessToken()', () => {
    it('deve retornar null quando não há token', () => {
      expect(service.getAccessToken()).toBeNull();
    });

    it('deve retornar o token do localStorage', () => {
      localStorage.setItem('access_token', 'mock-token');
      expect(service.getAccessToken()).toBe('mock-token');
    });
  });

  describe('logout()', () => {
    it('deve remover tokens do localStorage', () => {
      localStorage.setItem('access_token', 'mock-token');
      localStorage.setItem('refresh_token', 'mock-refresh');
      service.logout();
      const req = httpMock.expectOne(`${environment.apiUrl}/auth/logout`);
      req.flush({});
      expect(localStorage.getItem('access_token')).toBeNull();
      expect(localStorage.getItem('refresh_token')).toBeNull();
    });
  });
});