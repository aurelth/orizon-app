import { Routes } from '@angular/router';

export const authRoutes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./login/login').then((m) => m.LoginComponent),
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./register/register').then((m) => m.RegisterComponent),
  },
  {
    path: 'callback',
    loadComponent: () =>
      import('./oauth-callback/oauth-callback').then(
        (m) => m.OAuthCallbackComponent
      ),
  },
  {
    path: 'forgot-password',
    loadComponent: () =>
      import('./forgot-password/forgot-password').then(
        (m) => m.ForgotPasswordComponent
      ),
  },
  {
    path: 'reset-password',
    loadComponent: () =>
      import('./reset-password/reset-password').then(
        (m) => m.ResetPasswordComponent
      ),
  },
  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full',
  },
];