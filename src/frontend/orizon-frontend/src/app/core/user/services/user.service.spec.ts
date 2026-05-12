import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { UserService } from './user.service';
import { UserStore } from '../store/user.store';
import { ApiService } from '../../http/api.service';
import { of, throwError } from 'rxjs';

describe('UserService', () => {
  let service: UserService;
  let apiService: jest.Mocked<Partial<ApiService>>;
  let store: InstanceType<typeof UserStore>;

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
    googleConnected: true,
    trelloEnabled: false,
  };

  beforeEach(() => {
    apiService = {
      get: jest.fn(),
      put: jest.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        UserStore,
        { provide: ApiService, useValue: apiService },
      ],
    });

    service = TestBed.inject(UserService);
    store = TestBed.inject(UserStore);
  });

  it('deve ser criado', () => {
    expect(service).toBeTruthy();
  });

  it('deve carregar perfil e atualizar store', () => {
    (apiService.get as jest.Mock).mockReturnValue(of(mockProfile));

    service.getProfile().subscribe();

    expect(store.profile()).toEqual(mockProfile);
    expect(store.isLoading()).toBe(false);
  });

  it('deve aplicar tema claro no html ao carregar perfil com tema Light', () => {
    (apiService.get as jest.Mock).mockReturnValue(
      of({ ...mockProfile, themePreference: 'Light' })
    );

    service.getProfile().subscribe();

    expect(document.documentElement.classList.contains('theme-light')).toBe(true);
    document.documentElement.classList.remove('theme-light');
  });

  it('deve remover tema claro ao carregar perfil com tema Dark', () => {
    document.documentElement.classList.add('theme-light');
    (apiService.get as jest.Mock).mockReturnValue(of(mockProfile));

    service.getProfile().subscribe();

    expect(document.documentElement.classList.contains('theme-light')).toBe(false);
  });

  it('deve definir erro no store quando getProfile falhar', () => {
    (apiService.get as jest.Mock).mockReturnValue(
      throwError(() => new Error('Network error'))
    );

    service.getProfile().subscribe({ error: () => {} });

    expect(store.error()).toBe('Falha ao carregar perfil.');
  });

  it('deve atualizar perfil com sucesso', () => {
    (apiService.put as jest.Mock).mockReturnValue(of(void 0));

    service.updateProfile({
      displayName: 'Aurel Lossou',
      profilePictureUrl: null,
      themePreference: 'Dark',
    }).subscribe();

    expect(apiService.put).toHaveBeenCalledWith('/users/profile', {
      displayName: 'Aurel Lossou',
      profilePictureUrl: null,
      themePreference: 'Dark',
    });
  });

  it('deve aplicar tema claro ao atualizar para Light', () => {
    (apiService.put as jest.Mock).mockReturnValue(of(void 0));

    service.updateProfile({
      displayName: 'Aurel',
      profilePictureUrl: null,
      themePreference: 'Light',
    }).subscribe();

    expect(document.documentElement.classList.contains('theme-light')).toBe(true);
    document.documentElement.classList.remove('theme-light');
  });

  it('deve remover tema claro ao atualizar para Dark', () => {
    document.documentElement.classList.add('theme-light');
    (apiService.put as jest.Mock).mockReturnValue(of(void 0));

    service.updateProfile({
      displayName: 'Aurel',
      profilePictureUrl: null,
      themePreference: 'Dark',
    }).subscribe();

    expect(document.documentElement.classList.contains('theme-light')).toBe(false);
  });

  it('deve definir erro no store quando updateProfile falhar', () => {
    (apiService.put as jest.Mock).mockReturnValue(
      throwError(() => new Error('Server error'))
    );

    service.updateProfile({
      displayName: 'Aurel',
      profilePictureUrl: null,
      themePreference: 'Dark',
    }).subscribe({ error: () => {} });

    expect(store.error()).toBe('Falha ao atualizar perfil.');
  });
});