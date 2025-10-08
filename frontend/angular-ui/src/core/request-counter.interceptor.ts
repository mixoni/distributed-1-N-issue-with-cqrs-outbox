import { HttpEvent, HttpEventType, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { tap } from 'rxjs/operators';
import { RequestCounterService } from './request-counter.service';

export const requestCounterInterceptor: HttpInterceptorFn = (req, next) => {
  const counter = inject(RequestCounterService);
  counter.record(req.url); // brojimo klijentske pozive (1 ka BFF)

  return next(req).pipe(
    tap((ev: HttpEvent<any>) => {
      if (ev.type === HttpEventType.Response) {
        const h = ev.headers;
        const getInt = (k: string) => {
          const v = h.get(k);
          return v ? parseInt(v, 10) : undefined;
        };

        // BFF metrics
        const total = getInt('X-Metrics-Total');
        if (total !== undefined) {
          counter.total.set(total);
          counter.customersById.set(getInt('X-Metrics-CustomersById') ?? 0);
          counter.customersBatch.set(getInt('X-Metrics-CustomersBatch') ?? 0);
          counter.ordersList.set(getInt('X-Metrics-OrdersList') ?? 0);
          counter.ordersRead.set(getInt('X-Metrics-OrdersRead') ?? 0);
        }
      }
    })
  );
};
