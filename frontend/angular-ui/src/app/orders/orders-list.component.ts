import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OrdersService } from './orders.service';
@Component({
  selector: 'orders-list', standalone: true, imports: [CommonModule],
  templateUrl: './orders-list.component.html'
})
export class OrdersListComponent implements OnInit {
  mode = signal<'naive'|'batched'|'read'>('read');
  data = signal<any[]>([]); loading = signal(false); error = signal<string|null>(null);
  constructor(private svc: OrdersService) {}
  ngOnInit(){ this.refresh(); }
  setMode(m:'naive'|'batched'|'read'){ this.mode.set(m); this.refresh(); }
  refresh(){
    this.loading.set(true); this.error.set(null);
    const call = this.mode()==='naive'? this.svc.listNaive(): this.mode()==='batched'? this.svc.listBatched(): this.svc.listRead();
    call.subscribe({ next:d=>{ this.data.set(d); this.loading.set(false); }, error:e=>{ this.error.set(e.message||'Error'); this.loading.set(false); } });
  }
}
