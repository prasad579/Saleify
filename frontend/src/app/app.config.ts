import { ApplicationConfig, APP_INITIALIZER, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { routes } from './app.routes';
import { ApiHealthService } from '@core/services/api-health.service';
import { EngagementConfigService } from '@core/services/engagement-config.service';
import { actingUserInterceptor } from '@core/interceptors/acting-user.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withInterceptors([actingUserInterceptor])),
    {
      provide: APP_INITIALIZER,
      multi: true,
      useFactory: (health: ApiHealthService) => () => health.check(),
      deps: [ApiHealthService]
    },
    {
      // Load the tenant's engagement-type catalog before the app renders.
      provide: APP_INITIALIZER,
      multi: true,
      useFactory: (cfg: EngagementConfigService) => () => cfg.load(),
      deps: [EngagementConfigService]
    }
  ]
};
