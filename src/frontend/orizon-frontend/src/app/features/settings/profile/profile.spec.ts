import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { ProfileComponent } from './profile';
import { UserService } from '../../../core/user/services/user.service';
import { UserStore } from '../../../core/user/store/user.store';
import { ToastService } from '../../../core/toast/toast.service';
import { of, throwError } from 'rxjs';

describe('ProfileComponent', () => {
  let component: ProfileComponent;
  let userService: jest.Mocked<Partial<UserService>>;
  let toastService: jest.Mocked<Partial<ToastService>>;

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
    hasCompletedOnboarding: true,
    briefingHour: 6,
    emailSectionEnabled: true,
    calendarSectionEnabled: true,
    trelloSectionEnabled: true,
    tasksSectionEnabled: true,
    weatherSectionEnabled: true,
  };

  beforeEach(async () => {
    userService = {
      getProfile: jest.fn().mockReturnValue(of(mockProfile)),
      updateProfile: jest.fn(),
      updateBriefingPreferences: jest.fn(),
    };

    toastService = {
      success: jest.fn(),
      error: jest.fn(),
    };

    await TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        UserStore,
        { provide: UserService, useValue: userService },
        { provide: ToastService, useValue: toastService },
      ],
    }).compileComponents();

    component = TestBed.runInInjectionContext(() => new ProfileComponent());
    component.ngOnInit();
  });

  it('deve ser criado', () => {
    expect(component).toBeTruthy();
  });

  it('deve inicializar o form com valores do perfil', () => {
    expect(component.profileForm.get('displayName')?.value).toBe('Aurel');
    expect(component.profileForm.get('themePreference')?.value).toBe('Dark');
  });

  it('deve inicializar o preferencesForm com valores do perfil', () => {
    expect(component.preferencesForm.get('briefingHour')?.value).toBe(6);
    expect(component.preferencesForm.get('emailSectionEnabled')?.value).toBe(true);
    expect(component.preferencesForm.get('calendarSectionEnabled')?.value).toBe(true);
    expect(component.preferencesForm.get('trelloSectionEnabled')?.value).toBe(true);
    expect(component.preferencesForm.get('tasksSectionEnabled')?.value).toBe(true);
    expect(component.preferencesForm.get('weatherSectionEnabled')?.value).toBe(true);
  });

  it('deve inicializar hasChanges como false', () => {
    expect(component.hasChanges()).toBe(false);
  });

  it('deve inicializar hasPreferencesChanges como false', () => {
    expect(component.hasPreferencesChanges()).toBe(false);
  });

  it('deve detectar mudanças ao alterar displayName', () => {
    component.profileForm.get('displayName')?.setValue('Novo Nome');
    component.profileForm.markAsDirty();
    component.hasChanges.set(true);
    expect(component.hasChanges()).toBe(true);
  });

  it('deve detectar mudanças ao chamar setTheme', () => {
    component.setTheme('Light');
    expect(component.hasChanges()).toBe(true);
    expect(component.profileForm.get('themePreference')?.value).toBe('Light');
  });

  it('não deve chamar updateProfile quando form inválido', () => {
    component.profileForm.get('displayName')?.setValue('');
    component.onSubmit();
    expect(userService.updateProfile).not.toHaveBeenCalled();
  });

  it('não deve chamar updateProfile quando não há mudanças', () => {
    component.onSubmit();
    expect(userService.updateProfile).not.toHaveBeenCalled();
  });

  it('deve chamar updateProfile com dados corretos', () => {
    (userService.updateProfile as jest.Mock).mockReturnValue(of(void 0));
    component.profileForm.get('displayName')?.setValue('Aurel Lossou');
    component.profileForm.markAsDirty();
    component.hasChanges.set(true);
    component.onSubmit();
    expect(userService.updateProfile).toHaveBeenCalledWith({
      displayName: 'Aurel Lossou',
      profilePictureUrl: null,
      themePreference: 'Dark',
    });
  });

  it('deve chamar toast.success após salvar perfil com sucesso', () => {
    (userService.updateProfile as jest.Mock).mockReturnValue(of(void 0));
    component.profileForm.get('displayName')?.setValue('Aurel Lossou');
    component.profileForm.markAsDirty();
    component.hasChanges.set(true);
    component.onSubmit();
    expect(toastService.success).toHaveBeenCalledWith('Perfil atualizado com sucesso.');
  });

  it('deve chamar toast.error quando salvar perfil falhar', () => {
    (userService.updateProfile as jest.Mock).mockReturnValue(
      throwError(() => new Error('error'))
    );
    component.profileForm.get('displayName')?.setValue('Aurel Lossou');
    component.profileForm.markAsDirty();
    component.hasChanges.set(true);
    component.onSubmit();
    expect(toastService.error).toHaveBeenCalledWith('Erro ao atualizar perfil.');
  });

  it('não deve chamar updateBriefingPreferences quando não há mudanças', () => {
    component.onSubmitPreferences();
    expect(userService.updateBriefingPreferences).not.toHaveBeenCalled();
  });

  it('deve chamar updateBriefingPreferences com dados corretos', () => {
    (userService.updateBriefingPreferences as jest.Mock).mockReturnValue(of(void 0));
    component.preferencesForm.get('briefingHour')?.setValue(8);
    component.preferencesForm.get('emailSectionEnabled')?.setValue(false);
    component.preferencesForm.markAsDirty();
    component.hasPreferencesChanges.set(true);
    component.onSubmitPreferences();
    expect(userService.updateBriefingPreferences).toHaveBeenCalledWith({
      briefingHour: 8,
      emailSectionEnabled: false,
      calendarSectionEnabled: true,
      trelloSectionEnabled: true,
      tasksSectionEnabled: true,
      weatherSectionEnabled: true,
    });
  });

  it('deve chamar toast.success após salvar preferências com sucesso', () => {
    (userService.updateBriefingPreferences as jest.Mock).mockReturnValue(of(void 0));
    component.preferencesForm.markAsDirty();
    component.hasPreferencesChanges.set(true);
    component.onSubmitPreferences();
    expect(toastService.success).toHaveBeenCalledWith('Preferências de briefing atualizadas.');
  });

  it('deve chamar toast.error quando salvar preferências falhar', () => {
    (userService.updateBriefingPreferences as jest.Mock).mockReturnValue(
      throwError(() => new Error('error'))
    );
    component.preferencesForm.markAsDirty();
    component.hasPreferencesChanges.set(true);
    component.onSubmitPreferences();
    expect(toastService.error).toHaveBeenCalledWith('Erro ao atualizar preferências.');
  });

  it('deve ter 24 horas disponíveis', () => {
    expect(component.availableHours).toHaveLength(24);
    expect(component.availableHours[0]).toEqual({ value: 0, label: '00:00' });
    expect(component.availableHours[6]).toEqual({ value: 6, label: '06:00' });
    expect(component.availableHours[23]).toEqual({ value: 23, label: '23:00' });
  });

  it('deve setar isSaved como true após salvar perfil', () => {
    (userService.updateProfile as jest.Mock).mockReturnValue(of(void 0));
    component.profileForm.get('displayName')?.setValue('Aurel Lossou');
    component.profileForm.markAsDirty();
    component.hasChanges.set(true);
    component.onSubmit();
    expect(component.isSaved()).toBe(true);
  });

  it('deve resetar hasPreferencesChanges após salvar preferências', () => {
    (userService.updateBriefingPreferences as jest.Mock).mockReturnValue(of(void 0));
    component.preferencesForm.markAsDirty();
    component.hasPreferencesChanges.set(true);
    component.onSubmitPreferences();
    expect(component.hasPreferencesChanges()).toBe(false);
  });
});