import { bootstrapApplication } from '@angular/platform-browser';
import { provideHttpClient, withInterceptors, withInterceptorsFromDi } from '@angular/common/http';
import { AppComponent } from './app/app.component';
import { requestCounterInterceptor } from './core/request-counter.interceptor';

bootstrapApplication(AppComponent, {
  providers: [
    provideHttpClient(withInterceptors([requestCounterInterceptor]))
  ]
}).catch(err => console.error(err));
