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
    path: '',
    redirectTo: 'login',
    pathMatch: 'full',
  },
];