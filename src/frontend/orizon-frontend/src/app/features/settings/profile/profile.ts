import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
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

  readonly profile = this.store.profile;
  readonly isLoading = this.store.isLoading;
  readonly error = this.store.error;

  profileForm!: FormGroup;
  isSaving = signal(false);
  isSaved = signal(false);
  hasChanges = signal(false);

  ngOnInit(): void {
    this.profileForm = this.fb.group({
      displayName: ['', [Validators.required, Validators.minLength(2)]],
      themePreference: ['Dark'],
    });

    this.userService.getProfile().subscribe({
      next: (profile) => {
        this.profileForm.patchValue({
          displayName: profile.displayName,
          themePreference: profile.themePreference,
        });

        this.profileForm.valueChanges.subscribe(() => {
          this.hasChanges.set(this.profileForm.dirty);
        });
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
}