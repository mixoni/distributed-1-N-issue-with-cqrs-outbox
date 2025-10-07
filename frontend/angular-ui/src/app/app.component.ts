import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OrdersListComponent } from './orders/orders-list.component';
@Component({
  selector: 'app-root', standalone: true, imports: [CommonModule, OrdersListComponent],
  template: <h1>N+1 â†’ Batched â†’ CQRS Read (Angular + .NET)</h1>
             <orders-list></orders-list>
             <p><small class="muted">Proxy: <code>/api</code> â†’ <code>http://localhost:5000</code> (BFF)</small></p>
})
export class AppComponent {}
