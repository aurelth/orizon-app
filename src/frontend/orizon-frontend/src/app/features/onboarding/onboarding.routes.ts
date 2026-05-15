import { Routes } from '@angular/router';

export const onboardingRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./onboarding').then((m) => m.OnboardingComponent),
  },
];