import { Routes } from '@angular/router';

export const settingsRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./layout/settings-layout').then((m) => m.SettingsLayoutComponent),
    children: [
      {
        path: 'integrations',
        loadComponent: () =>
          import('./integrations/integrations').then((m) => m.IntegrationsComponent),
      },
      {
        path: 'location',
        loadComponent: () =>
          import('./location/location').then((m) => m.LocationComponent),
      },
      {
        path: 'travel-mode',
        loadComponent: () =>
          import('./travel-mode/travel-mode').then((m) => m.TravelModeComponent),
      },
      {
        path: '',
        redirectTo: 'integrations',
        pathMatch: 'full',
      },
    ],
  },
];