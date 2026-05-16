import { ApplicationConfig, LOCALE_ID, importProvidersFrom } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { ToastrModule } from 'ngx-toastr';
import { routes } from './app.routes';
import { jwtInterceptor } from './core/auth/interceptors/jwt.interceptor';
import { refreshTokenInterceptor } from './core/auth/interceptors/refresh-token.interceptor';
import { registerLocaleData } from '@angular/common';
import localePtBr from '@angular/common/locales/pt';

registerLocaleData(localePtBr);

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(
      withInterceptors([jwtInterceptor, refreshTokenInterceptor])
    ),
    provideAnimations(),
    importProvidersFrom(
      ToastrModule.forRoot({
        timeOut: 4000,
        positionClass: 'toast-bottom-right',
        preventDuplicates: true,
        progressBar: true,
        closeButton: true,
      })
    ),
    { provide: LOCALE_ID, useValue: 'pt' },
  ],
};