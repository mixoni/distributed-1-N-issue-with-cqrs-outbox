import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
@Injectable({ providedIn: 'root' })
export class OrdersService {
  constructor(private http: HttpClient) {}
  listNaive(): Observable<any[]> { return this.http.get<any[]>('/api/orders/v1/naive'); }
  listBatched(): Observable<any[]> { return this.http.get<any[]>('/api/orders/v1/batched'); }
  listRead(): Observable<any[]> { return this.http.get<any[]>('/api/orders/v2/read-model'); }
}
