import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { UserService } from '../../core/user/services/user.service';
import { ApiService } from '../../core/http/api.service';

interface OnboardingStep {
  id: number;
  title: string;
  subtitle: string;
  icon: string;
}

@Component({
  selector: 'app-onboarding',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './onboarding.html',
  styleUrl: './onboarding.scss',
})
export class OnboardingComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly userService = inject(UserService);
  private readonly api = inject(ApiService);

  readonly currentStep = signal(1);
  readonly isCompleting = signal(false);

  readonly steps: OnboardingStep[] = [
    {
      id: 1,
      title: 'Bem-vindo ao Orizon',
      subtitle: 'Seu briefing diário personalizado',
      icon: '🌅',
    },
    {
      id: 2,
      title: 'Localização',
      subtitle: 'Para mostrar o clima do seu dia',
      icon: '📍',
    },
    {
      id: 3,
      title: 'Google',
      subtitle: 'Gmail, Calendar e Tasks',
      icon: '🔗',
    },
    {
      id: 4,
      title: 'Trello',
      subtitle: 'Suas tarefas e projetos',
      icon: '📋',
    },
    {
      id: 5,
      title: 'Tudo pronto!',
      subtitle: 'Gere seu primeiro briefing',
      icon: '🎉',
    },
  ];

  readonly totalSteps = this.steps.length;
  readonly progressPercent = computed(
    () => ((this.currentStep() - 1) / (this.totalSteps - 1)) * 100
  );

  readonly isFirstStep = computed(() => this.currentStep() === 1);
  readonly isLastStep = computed(() => this.currentStep() === this.totalSteps);

  ngOnInit(): void {
    this.userService.getProfile().subscribe({
      next: (profile) => {
        if (profile.hasCompletedOnboarding) {
          this.router.navigate(['/dashboard']);
        }
      },
    });
  }

  next(): void {
    if (this.currentStep() < this.totalSteps) {
      this.currentStep.update((s) => s + 1);
    }
  }

  back(): void {
    if (this.currentStep() > 1) {
      this.currentStep.update((s) => s - 1);
    }
  }

  goToStep(step: number): void {
    if (step >= 1 && step <= this.totalSteps) {
      this.currentStep.set(step);
    }
  }

  completeOnboarding(): void {
    this.isCompleting.set(true);
    this.api.post('/users/onboarding/complete', {}).subscribe({
      next: () => {
        this.isCompleting.set(false);
        this.router.navigate(['/dashboard']);
      },
      error: () => {
        this.isCompleting.set(false);
        this.router.navigate(['/dashboard']);
      },
    });
  }
}