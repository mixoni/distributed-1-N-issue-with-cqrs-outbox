import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OrdersService } from './orders.service';
import { RequestCounterService } from 'src/core/request-counter.service';

@Component({
  selector: 'orders-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './orders-list.component.html'
})
export class OrdersListComponent implements OnInit {
  mode = signal<'naive'|'batched'|'read'>('read');
  data = signal<any[]>([]);
  loading = signal(false);
  error = signal<string|null>(null);
  elapsedMs = signal<number>(0);

  // preuzimamo brojače iz servisa (computed radi auto-refresh u template-u)
  totalReq = computed(() => this.counter.total());
  custByIdReq = computed(() => this.counter.customersById());
  custBatchReq = computed(() => this.counter.customersBatch());
  ordersListReq = computed(() => this.counter.ordersList());
  ordersReadReq = computed(() => this.counter.ordersRead());

  constructor(private svc: OrdersService, private counter: RequestCounterService) {}

  ngOnInit(){ this.refresh(); }

  setMode(m:'naive'|'batched'|'read'){ this.mode.set(m); this.refresh(); }

  refresh(){
    this.counter.reset();
    this.loading.set(true);
    this.error.set(null);
    this.elapsedMs.set(0);
    const start = performance.now();

    const call = this.mode()==='naive'
      ? this.svc.listNaive()
      : this.mode()==='batched'
        ? this.svc.listBatched()
        : this.svc.listRead();

    call.subscribe({
      next: d => { this.data.set(d); },
      error: e => { this.error.set(e?.message || 'Error'); },
      complete: () => {
        const end = performance.now();
        this.elapsedMs.set(Math.round(end - start));
        this.loading.set(false);
      }
    });
  }
}
