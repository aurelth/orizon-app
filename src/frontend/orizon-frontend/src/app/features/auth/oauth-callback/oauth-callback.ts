import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-oauth-callback',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="callback-container">
      <div class="callback-content">
        <i class="pi pi-spinner pi-spin"></i>
        <p>Autenticando...</p>
      </div>
    </div>
  `,
  styles: [`
    .callback-container {
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 100vh;
      background-color: var(--color-bg);
    }

    .callback-content {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 1rem;
      color: var(--color-text-muted);

      i {
        font-size: 2rem;
        color: var(--color-primary);
      }

      p {
        font-size: 1rem;
      }
    }
  `],
})
export class OAuthCallbackComponent implements OnInit {
  private readonly router = inject(Router);

  ngOnInit(): void {
    // Será implementado na Fase 6 com o GoogleOAuthService
    // Por enquanto redireciona para login
    this.router.navigate(['/auth/login']);
  }
}