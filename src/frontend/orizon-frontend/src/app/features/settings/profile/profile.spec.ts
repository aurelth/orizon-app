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
    hasCompletedOnboarding: true,
  };

  beforeEach(async () => {
    userService = {
      getProfile: jest.fn().mockReturnValue(of(mockProfile)),
      updateProfile: jest.fn(),
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

    store = TestBed.inject(UserStore);
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

  it('deve inicializar hasChanges como false', () => {
    expect(component.hasChanges()).toBe(false);
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

  it('deve chamar toast.success após salvar com sucesso', () => {
    (userService.updateProfile as jest.Mock).mockReturnValue(of(void 0));
    component.profileForm.get('displayName')?.setValue('Aurel Lossou');
    component.profileForm.markAsDirty();
    component.hasChanges.set(true);
    component.onSubmit();

    expect(toastService.success).toHaveBeenCalledWith('Perfil atualizado com sucesso.');
  });

  it('deve chamar toast.error quando salvar falhar', () => {
    (userService.updateProfile as jest.Mock).mockReturnValue(
      throwError(() => new Error('error'))
    );
    component.profileForm.get('displayName')?.setValue('Aurel Lossou');
    component.profileForm.markAsDirty();
    component.hasChanges.set(true);
    component.onSubmit();

    expect(toastService.error).toHaveBeenCalledWith('Erro ao atualizar perfil.');
  });

  it('deve setar isSaved como true após salvar com sucesso', () => {
    (userService.updateProfile as jest.Mock).mockReturnValue(of(void 0));
    component.profileForm.get('displayName')?.setValue('Aurel Lossou');
    component.profileForm.markAsDirty();
    component.hasChanges.set(true);
    component.onSubmit();
    expect(component.isSaved()).toBe(true);
  });

  it('deve resetar hasChanges após salvar', () => {
    (userService.updateProfile as jest.Mock).mockReturnValue(of(void 0));
    component.profileForm.get('displayName')?.setValue('Aurel Lossou');
    component.profileForm.markAsDirty();
    component.hasChanges.set(true);
    component.onSubmit();
    expect(component.hasChanges()).toBe(false);
  });

  it('deve validar campo displayName inválido', () => {
    component.profileForm.get('displayName')?.setValue('');
    component.profileForm.get('displayName')?.markAsTouched();
    expect(component.isFieldInvalid('displayName')).toBe(true);
  });
});