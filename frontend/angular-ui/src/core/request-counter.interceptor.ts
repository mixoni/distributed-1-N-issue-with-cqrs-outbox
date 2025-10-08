import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { RequestCounterService } from './request-counter.service';

export const requestCounterInterceptor: HttpInterceptorFn = (req, next) => {
  const counter = inject(RequestCounterService);
  counter.record(req.url);
  return next(req);
};
