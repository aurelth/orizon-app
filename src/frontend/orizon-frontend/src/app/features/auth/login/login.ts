import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/auth/services/auth.service';
import { AuthStore } from '../../../core/auth/store/auth.store';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class LoginComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly authStore = inject(AuthStore);
  private readonly router = inject(Router);

  form!: FormGroup;
  showPassword = false;

  readonly isLoading = this.authStore.isLoading;
  readonly error = this.authStore.error;

  ngOnInit(): void {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]],
    });
  }

  togglePassword(): void {
    this.showPassword = !this.showPassword;
  }

  isFieldInvalid(field: string): boolean {
    const control = this.form.get(field);
    return !!(control?.invalid && control?.touched);
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.authStore.setLoading(true);
    this.authStore.clearError();

    this.authService.login(this.form.value).subscribe({
      next: () => {
        this.authStore.setLoading(false);
        this.router.navigate(['/settings']);
      },
      error: (err) => {
        this.authStore.setError(
          err.error?.message ?? 'Email ou senha inválidos.'
        );
      },
    });
  }
}