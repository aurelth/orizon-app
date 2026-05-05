import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { SettingsLayoutComponent } from './settings-layout';
import { AuthService } from '../../../core/auth/services/auth.service';
import { AuthStore } from '../../../core/auth/store/auth.store';

describe('SettingsLayoutComponent', () => {
  let component: SettingsLayoutComponent;
  let authService: jest.Mocked<Partial<AuthService>>;

  beforeEach(async () => {
    authService = {
      logout: jest.fn(),
      getAccessToken: jest.fn().mockReturnValue(null),
      getRefreshToken: jest.fn().mockReturnValue(null),
      isAuthenticated: jest.fn().mockReturnValue(true),
    };

    await TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        AuthStore,
        { provide: AuthService, useValue: authService },
      ],
    }).compileComponents();

    component = TestBed.runInInjectionContext(() => new SettingsLayoutComponent());
  });

  it('deve ser criado', () => {
    expect(component).toBeTruthy();
  });

  it('deve chamar authService.logout ao fazer logout', () => {
    component.logout();
    expect(authService.logout).toHaveBeenCalled();
  });
});