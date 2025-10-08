import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class RequestCounterService {
  total = signal(0);
  customersById = signal(0);
  customersBatch = signal(0);
  ordersList = signal(0);
  ordersRead = signal(0);

  reset() {
    this.total.set(0);
    this.customersById.set(0);
    this.customersBatch.set(0);
    this.ordersList.set(0);
    this.ordersRead.set(0);
  }

  record(url: string) {
    this.total.update(v => v + 1);
    const u = url.toLowerCase();
    if (u.includes('/api/customers/batch')) this.customersBatch.update(v => v + 1);
    else if (u.match(/\/api\/customers\/\d+/)) this.customersById.update(v => v + 1);
    else if (u.includes('/api/orders/read')) this.ordersRead.update(v => v + 1);
    else if (u.includes('/api/orders')) this.ordersList.update(v => v + 1);
  }
}
