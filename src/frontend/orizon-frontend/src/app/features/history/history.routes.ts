import { Routes } from '@angular/router';

export const historyRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./history').then((m) => m.HistoryComponent),
  },
  {
    path: ':date',
    loadComponent: () =>
      import('./history-detail/history-detail').then((m) => m.HistoryDetailComponent),
  },
];