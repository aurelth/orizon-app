import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { ProfileComponent } from './profile';
import { UserService } from '../../../core/user/services/user.service';
import { UserStore } from '../../../core/user/store/user.store';
import { ToastService } from '../../../core/toast/toast.service';
import { of, throwError } from 'rxjs';

describe('ProfileComponent', () => {
  let component: ProfileComponent;
  let userService: jest.Mocked<Partial<UserService>>;
  let toastService: jest.Mocked<Partial<ToastService>>;
  let router: jest.Mocked<Partial<Router>>;

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

  // Mock do FileReader para testes síncronos
  class MockFileReader {
    result: string | null = null;
    onload: ((ev: any) => any) | null = null;

    readAsDataURL(_file: Blob) {
      this.result = 'data:image/jpeg;base64,abc123';
      if (this.onload) {
        this.onload({ target: this });
      }
    }
  }

  beforeEach(async () => {
    (global as any).FileReader = MockFileReader;

    userService = {
      getProfile: jest.fn().mockReturnValue(of(mockProfile)),
      updateProfile: jest.fn(),
      updateBriefingPreferences: jest.fn(),
      changePassword: jest.fn(),
      deleteAccount: jest.fn(),
      uploadProfilePicture: jest.fn(),
    };

    toastService = {
      success: jest.fn(),
      error: jest.fn(),
    };

    router = {
      navigate: jest.fn().mockResolvedValue(true),
    };

    await TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        UserStore,
        { provide: UserService, useValue: userService },
        { provide: ToastService, useValue: toastService },
        { provide: Router, useValue: router },
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
  });

  it('deve inicializar o securityForm vazio', () => {
    expect(component.securityForm.get('currentPassword')?.value).toBe('');
    expect(component.securityForm.get('newPassword')?.value).toBe('');
    expect(component.securityForm.get('confirmNewPassword')?.value).toBe('');
  });

  it('deve inicializar o deleteForm vazio', () => {
    expect(component.deleteForm.get('password')?.value).toBe('');
  });

  it('deve inicializar isUploadingPhoto como false', () => {
    expect(component.isUploadingPhoto()).toBe(false);
  });

  it('deve inicializar previewUrl como null', () => {
    expect(component.previewUrl()).toBeNull();
  });

  it('deve inicializar hasChanges como false', () => {
    expect(component.hasChanges()).toBe(false);
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

  it('não deve chamar updateBriefingPreferences quando não há mudanças', () => {
    component.onSubmitPreferences();
    expect(userService.updateBriefingPreferences).not.toHaveBeenCalled();
  });

  it('deve chamar updateBriefingPreferences com dados corretos', () => {
    (userService.updateBriefingPreferences as jest.Mock).mockReturnValue(of(void 0));
    component.preferencesForm.get('briefingHour')?.setValue(8);
    component.preferencesForm.markAsDirty();
    component.hasPreferencesChanges.set(true);
    component.onSubmitPreferences();
    expect(userService.updateBriefingPreferences).toHaveBeenCalledWith({
      briefingHour: 8,
      emailSectionEnabled: true,
      calendarSectionEnabled: true,
      trelloSectionEnabled: true,
      tasksSectionEnabled: true,
      weatherSectionEnabled: true,
    });
  });

  // --- Alterar senha ---

  it('não deve chamar changePassword quando securityForm inválido', () => {
    component.onChangePassword();
    expect(userService.changePassword).not.toHaveBeenCalled();
  });

  it('não deve chamar changePassword quando senhas não coincidem', () => {
    component.securityForm.patchValue({
      currentPassword: 'Senha@123',
      newPassword: 'NovaSenha@456',
      confirmNewPassword: 'Diferente@789',
    });
    component.onChangePassword();
    expect(userService.changePassword).not.toHaveBeenCalled();
  });

  it('deve chamar changePassword com dados corretos', () => {
    (userService.changePassword as jest.Mock).mockReturnValue(of(void 0));
    component.securityForm.patchValue({
      currentPassword: 'Senha@123',
      newPassword: 'NovaSenha@456',
      confirmNewPassword: 'NovaSenha@456',
    });
    component.onChangePassword();
    expect(userService.changePassword).toHaveBeenCalledWith({
      currentPassword: 'Senha@123',
      newPassword: 'NovaSenha@456',
    });
  });

  it('deve chamar toast.success após alterar senha', () => {
    (userService.changePassword as jest.Mock).mockReturnValue(of(void 0));
    component.securityForm.patchValue({
      currentPassword: 'Senha@123',
      newPassword: 'NovaSenha@456',
      confirmNewPassword: 'NovaSenha@456',
    });
    component.onChangePassword();
    expect(toastService.success).toHaveBeenCalledWith('Senha alterada com sucesso.');
  });

  it('deve chamar toast.error quando alterar senha falhar', () => {
    (userService.changePassword as jest.Mock).mockReturnValue(throwError(() => new Error('error')));
    component.securityForm.patchValue({
      currentPassword: 'Senha@123',
      newPassword: 'NovaSenha@456',
      confirmNewPassword: 'NovaSenha@456',
    });
    component.onChangePassword();
    expect(toastService.error).toHaveBeenCalledWith(
      'Senha atual incorreta ou nova senha inválida.',
    );
  });

  it('passwordsMismatch deve retornar true quando senhas diferem', () => {
    component.securityForm.patchValue({
      newPassword: 'NovaSenha@456',
      confirmNewPassword: 'Diferente@789',
    });
    expect(component.passwordsMismatch()).toBe(true);
  });

  it('passwordsMismatch deve retornar false quando senhas coincidem', () => {
    component.securityForm.patchValue({
      newPassword: 'NovaSenha@456',
      confirmNewPassword: 'NovaSenha@456',
    });
    expect(component.passwordsMismatch()).toBe(false);
  });

  // --- Excluir conta ---

  it('não deve chamar deleteAccount quando deleteForm inválido', () => {
    component.onDeleteAccount();
    expect(userService.deleteAccount).not.toHaveBeenCalled();
  });

  it('deve chamar deleteAccount com senha correta', () => {
    (userService.deleteAccount as jest.Mock).mockReturnValue(of(void 0));
    component.deleteForm.get('password')?.setValue('Senha@123');
    component.onDeleteAccount();
    expect(userService.deleteAccount).toHaveBeenCalledWith({ password: 'Senha@123' });
  });

  it('deve navegar para login após excluir conta', () => {
    (userService.deleteAccount as jest.Mock).mockReturnValue(of(void 0));
    component.deleteForm.get('password')?.setValue('Senha@123');
    component.onDeleteAccount();
    expect(router.navigate).toHaveBeenCalledWith(['/auth/login']);
  });

  it('deve chamar toast.error quando deleteAccount falhar', () => {
    (userService.deleteAccount as jest.Mock).mockReturnValue(throwError(() => new Error('error')));
    component.deleteForm.get('password')?.setValue('SenhaErrada');
    component.onDeleteAccount();
    expect(toastService.error).toHaveBeenCalledWith('Senha incorreta. Conta não foi excluída.');
  });

  it('showDeleteConfirm deve iniciar como false', () => {
    expect(component.showDeleteConfirm()).toBe(false);
  });

  // --- Upload de foto (continuação) ---

  it('deve mostrar toast.error quando arquivo é muito grande', () => {
    const largeFile = new File([new ArrayBuffer(6 * 1024 * 1024)], 'photo.jpg', {
      type: 'image/jpeg',
    });
    const event = { target: { files: [largeFile], value: '' } } as any;
    component.onFileSelected(event);
    expect(toastService.error).toHaveBeenCalledWith('Arquivo muito grande. Tamanho máximo: 5MB.');
    expect(userService.uploadProfilePicture).not.toHaveBeenCalled();
  });

  it('deve mostrar toast.error quando tipo de arquivo não é permitido', () => {
    const gifFile = new File([new ArrayBuffer(100)], 'photo.gif', { type: 'image/gif' });
    const event = { target: { files: [gifFile], value: '' } } as any;
    component.onFileSelected(event);
    expect(toastService.error).toHaveBeenCalledWith('Tipo não permitido. Use JPG, PNG ou WebP.');
    expect(userService.uploadProfilePicture).not.toHaveBeenCalled();
  });

  it('deve chamar uploadProfilePicture com arquivo válido', () => {
    (userService.uploadProfilePicture as jest.Mock).mockReturnValue(
      of({ url: 'http://localhost:5010/uploads/profile-pictures/photo.jpg' }),
    );
    const validFile = new File([new ArrayBuffer(100)], 'photo.jpg', { type: 'image/jpeg' });
    const event = { target: { files: [validFile], value: '' } } as any;
    component.onFileSelected(event);
    expect(userService.uploadProfilePicture).toHaveBeenCalledWith(validFile);
  });

  it('deve definir previewUrl após selecionar arquivo válido', () => {
    (userService.uploadProfilePicture as jest.Mock).mockReturnValue(
      of({ url: 'http://localhost:5010/uploads/profile-pictures/photo.jpg' }),
    );
    const validFile = new File([new ArrayBuffer(100)], 'photo.jpg', { type: 'image/jpeg' });
    const event = { target: { files: [validFile], value: '' } } as any;
    component.onFileSelected(event);
    expect(component.previewUrl()).toBe('data:image/jpeg;base64,abc123');
  });

  it('deve chamar toast.success após upload bem-sucedido', () => {
    (userService.uploadProfilePicture as jest.Mock).mockReturnValue(
      of({ url: 'http://localhost:5010/uploads/profile-pictures/photo.jpg' }),
    );
    const validFile = new File([new ArrayBuffer(100)], 'photo.jpg', { type: 'image/jpeg' });
    const event = { target: { files: [validFile], value: '' } } as any;
    component.onFileSelected(event);
    expect(toastService.success).toHaveBeenCalledWith('Foto de perfil atualizada.');
  });

  it('deve chamar toast.error e limpar previewUrl quando upload falhar', () => {
    (userService.uploadProfilePicture as jest.Mock).mockReturnValue(
      throwError(() => new Error('error')),
    );
    const validFile = new File([new ArrayBuffer(100)], 'photo.jpg', { type: 'image/jpeg' });
    const event = { target: { files: [validFile], value: '' } } as any;
    component.onFileSelected(event);
    expect(toastService.error).toHaveBeenCalledWith('Erro ao fazer upload da foto.');
    expect(component.previewUrl()).toBeNull();
  });

  it('não deve fazer nada quando nenhum arquivo é selecionado', () => {
    const event = { target: { files: [], value: '' } } as any;
    component.onFileSelected(event);
    expect(userService.uploadProfilePicture).not.toHaveBeenCalled();
  });

  it('deve ter 24 horas disponíveis no seletor', () => {
    expect(component.availableHours).toHaveLength(24);
    expect(component.availableHours[0]).toEqual({ value: 0, label: '00:00' });
    expect(component.availableHours[6]).toEqual({ value: 6, label: '06:00' });
  });
});
