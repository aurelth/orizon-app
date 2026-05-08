import { Component, OnInit, inject } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-oauth-callback',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="callback-container">
      <div class="callback-content">
        @if (error) {
          <i class="pi pi-times-circle" style="color: var(--color-error)"></i>
          <p>{{ error }}</p>
          <button class="btn-back" (click)="goToSettings()">Voltar às configurações</button>
        } @else {
          <i class="pi pi-spinner pi-spin"></i>
          <p>Conectando sua conta Google...</p>
        }
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

    .btn-back {
      margin-top: 0.5rem;
      padding: 0.625rem 1.25rem;
      background: var(--color-bg-surface);
      border: 1px solid var(--color-border);
      border-radius: var(--radius-md);
      color: var(--color-text);
      font-size: 0.875rem;
      cursor: pointer;
      transition: border-color var(--transition);

      &:hover {
        border-color: var(--color-primary);
      }
    }
  `],
})
export class OAuthCallbackComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  error: string | null = null;

  ngOnInit(): void {
    const errorParam = this.route.snapshot.queryParamMap.get('error');

    if (errorParam) {
      this.error = 'Autorização negada pelo Google.';
      return;
    }
    
    setTimeout(() => {
      this.router.navigate(['/settings/integrations']);
    }, 1000);
  }

  goToSettings(): void {
    this.router.navigate(['/settings/integrations']);
  }
}