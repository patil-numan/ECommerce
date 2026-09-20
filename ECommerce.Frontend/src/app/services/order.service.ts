import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { CreateOrder } from '../models/create-order';
import { Order } from '../models/order';

@Injectable({
  providedIn: 'root'
})
export class OrderService {
  private readonly apiUrl =
    'https://localhost:7165/api/Orders';

  constructor(
    private readonly http: HttpClient
  ) {}

  createOrder(
    order: CreateOrder
  ): Observable<Order> {
    return this.http.post<Order>(
      this.apiUrl,
      order
    );
  }

  getMyOrders(): Observable<Order[]> {
    return this.http.get<Order[]>(
      this.apiUrl
    );
  }

  getOrderById(
    id: number
  ): Observable<Order> {
    return this.http.get<Order>(
      `${this.apiUrl}/${id}`
    );
  }

  getAdminOrders(): Observable<Order[]> {
    return this.http.get<Order[]>(
      `${this.apiUrl}/admin`
    );
  }

  getAdminOrderById(
    id: number
  ): Observable<Order> {
    return this.http.get<Order>(
      `${this.apiUrl}/admin/${id}`
    );
  }

  updateOrderStatus(
    id: number,
    status: string
  ): Observable<Order> {
    return this.http.put<Order>(
      `${this.apiUrl}/admin/${id}/status`,
      { status }
    );
  }
}