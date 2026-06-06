import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { UserService } from '../../../core/user/services/user.service';
import { UserStore } from '../../../core/user/store/user.store';
import { ToastService } from '../../../core/toast/toast.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './profile.html',
  styleUrl: './profile.scss',
})
export class ProfileComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly userService = inject(UserService);
  private readonly store = inject(UserStore);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  readonly profile = this.store.profile;
  readonly isLoading = this.store.isLoading;
  readonly error = this.store.error;

  profileForm!: FormGroup;
  preferencesForm!: FormGroup;
  securityForm!: FormGroup;
  deleteForm!: FormGroup;

  isSaving = signal(false);
  isSaved = signal(false);
  hasChanges = signal(false);

  isSavingPreferences = signal(false);
  isSavedPreferences = signal(false);
  hasPreferencesChanges = signal(false);

  isSavingPassword = signal(false);
  isSavedPassword = signal(false);

  isDeletingAccount = signal(false);
  showDeleteConfirm = signal(false);

  isUploadingPhoto = signal(false);
  previewUrl = signal<string | null>(null);

  readonly availableHours = Array.from({ length: 24 }, (_, i) => ({
    value: i,
    label: `${String(i).padStart(2, '0')}:00`,
  }));

  private readonly allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/webp'];
  private readonly maxSizeBytes = 5 * 1024 * 1024; // 5MB

  ngOnInit(): void {
    this.profileForm = this.fb.group({
      displayName: ['', [Validators.required, Validators.minLength(2)]],
      themePreference: ['Dark'],
    });

    this.preferencesForm = this.fb.group({
      briefingHour: [6],
      emailSectionEnabled: [true],
      calendarSectionEnabled: [true],
      trelloSectionEnabled: [true],
      tasksSectionEnabled: [true],
      weatherSectionEnabled: [true],
    });

    this.securityForm = this.fb.group({
      currentPassword: ['', [Validators.required]],
      newPassword: ['', [Validators.required, Validators.minLength(8)]],
      confirmNewPassword: ['', [Validators.required]],
    });

    this.deleteForm = this.fb.group({
      password: ['', [Validators.required]],
    });

    this.userService.getProfile().subscribe({
      next: (profile) => {
        this.profileForm.patchValue({
          displayName: profile.displayName,
          themePreference: profile.themePreference,
        });

        this.preferencesForm.patchValue({
          briefingHour: profile.briefingHour,
          emailSectionEnabled: profile.emailSectionEnabled,
          calendarSectionEnabled: profile.calendarSectionEnabled,
          trelloSectionEnabled: profile.trelloSectionEnabled,
          tasksSectionEnabled: profile.tasksSectionEnabled,
          weatherSectionEnabled: profile.weatherSectionEnabled,
        });

        this.profileForm.valueChanges.subscribe(() => {
          this.hasChanges.set(this.profileForm.dirty);
        });

        this.preferencesForm.valueChanges.subscribe(() => {
          this.hasPreferencesChanges.set(this.preferencesForm.dirty);
        });
      },
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;

    const file = input.files[0];

    if (file.size > this.maxSizeBytes) {
      this.toast.error('Arquivo muito grande. Tamanho máximo: 5MB.');
      input.value = '';
      return;
    }

    if (!this.allowedTypes.includes(file.type)) {
      this.toast.error('Tipo não permitido. Use JPG, PNG ou WebP.');
      input.value = '';
      return;
    }

    // Preview local antes do upload
    const reader = new FileReader();
    reader.onload = (e) => {
      this.previewUrl.set(e.target?.result as string);
    };
    reader.readAsDataURL(file);

    // Faz o upload
    this.isUploadingPhoto.set(true);
    this.userService.uploadProfilePicture(file).subscribe({
      next: () => {
        this.isUploadingPhoto.set(false);
        this.toast.success('Foto de perfil atualizada.');
        input.value = '';
      },
      error: () => {
        this.isUploadingPhoto.set(false);
        this.previewUrl.set(null);
        this.toast.error('Erro ao fazer upload da foto.');
        input.value = '';
      },
    });
  }

  setTheme(theme: 'Dark' | 'Light'): void {
    this.profileForm.get('themePreference')?.setValue(theme);
    this.profileForm.markAsDirty();
    this.hasChanges.set(true);
  }

  isFieldInvalid(field: string): boolean {
    const control = this.profileForm.get(field);
    return !!(control?.invalid && control?.touched);
  }

  isSecurityFieldInvalid(field: string): boolean {
    const control = this.securityForm.get(field);
    return !!(control?.invalid && control?.touched);
  }

  passwordsMismatch(): boolean {
    const newPassword = this.securityForm.get('newPassword')?.value;
    const confirmNewPassword = this.securityForm.get('confirmNewPassword')?.value;
    return newPassword !== confirmNewPassword && !!confirmNewPassword;
  }

  onSubmit(): void {
    if (this.profileForm.invalid || !this.hasChanges()) return;

    this.isSaving.set(true);
    const { displayName, themePreference } = this.profileForm.value;

    this.userService.updateProfile({
      displayName,
      profilePictureUrl: null,
      themePreference,
    }).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.isSaved.set(true);
        this.hasChanges.set(false);
        this.profileForm.markAsPristine();
        this.toast.success('Perfil atualizado com sucesso.');
        setTimeout(() => this.isSaved.set(false), 3000);
      },
      error: () => {
        this.isSaving.set(false);
        this.toast.error('Erro ao atualizar perfil.');
      },
    });
  }

  onSubmitPreferences(): void {
    if (!this.hasPreferencesChanges()) return;

    this.isSavingPreferences.set(true);
    const {
      briefingHour,
      emailSectionEnabled,
      calendarSectionEnabled,
      trelloSectionEnabled,
      tasksSectionEnabled,
      weatherSectionEnabled,
    } = this.preferencesForm.value;

    this.userService.updateBriefingPreferences({
      briefingHour,
      emailSectionEnabled,
      calendarSectionEnabled,
      trelloSectionEnabled,
      tasksSectionEnabled,
      weatherSectionEnabled,
    }).subscribe({
      next: () => {
        this.isSavingPreferences.set(false);
        this.isSavedPreferences.set(true);
        this.hasPreferencesChanges.set(false);
        this.preferencesForm.markAsPristine();
        this.toast.success('Preferências de briefing atualizadas.');
        setTimeout(() => this.isSavedPreferences.set(false), 3000);
      },
      error: () => {
        this.isSavingPreferences.set(false);
        this.toast.error('Erro ao atualizar preferências.');
      },
    });
  }

  onChangePassword(): void {
    if (this.securityForm.invalid || this.passwordsMismatch()) return;

    const { currentPassword, newPassword } = this.securityForm.value;
    this.isSavingPassword.set(true);

    this.userService.changePassword({ currentPassword, newPassword }).subscribe({
      next: () => {
        this.isSavingPassword.set(false);
        this.isSavedPassword.set(true);
        this.securityForm.reset();
        this.toast.success('Senha alterada com sucesso.');
        setTimeout(() => this.isSavedPassword.set(false), 3000);
      },
      error: () => {
        this.isSavingPassword.set(false);
        this.toast.error('Senha atual incorreta ou nova senha inválida.');
      },
    });
  }

  onDeleteAccount(): void {
    if (this.deleteForm.invalid) return;

    const { password } = this.deleteForm.value;
    this.isDeletingAccount.set(true);

    this.userService.deleteAccount({ password }).subscribe({
      next: () => {
        localStorage.clear();
        this.router.navigate(['/auth/login']);
      },
      error: () => {
        this.isDeletingAccount.set(false);
        this.toast.error('Senha incorreta. Conta não foi excluída.');
      },
    });
  }
}