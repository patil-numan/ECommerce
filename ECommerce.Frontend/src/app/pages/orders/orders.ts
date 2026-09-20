import {
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit
} from '@angular/core';

import {
  DecimalPipe,
  DatePipe
} from '@angular/common';

import {
  RouterLink
} from '@angular/router';

import { Subscription } from 'rxjs';

import { Navbar } from '../../components/navbar/navbar';

import { Order } from '../../models/order';
import { OrderService } from '../../services/order.service';

@Component({
  selector: 'app-orders',
  imports: [
    DecimalPipe,
    DatePipe,
    RouterLink,
    Navbar
  ],
  templateUrl: './orders.html',
  styleUrl: './orders.css'
})
export class Orders implements OnInit, OnDestroy {
  orders: Order[] = [];

  loading = true;
  error = '';

  private ordersSubscription?: Subscription;

  constructor(
    private readonly orderService: OrderService,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadOrders();
  }

  ngOnDestroy(): void {
    this.ordersSubscription?.unsubscribe();
  }

  private loadOrders(): void {
    this.loading = true;
    this.error = '';

    console.log('Loading my orders...');

    this.ordersSubscription =
      this.orderService.getMyOrders().subscribe({
        next: (orders: Order[]) => {
          console.log('Orders received:', orders);

          this.orders = orders;
          this.loading = false;

          this.changeDetectorRef.detectChanges();
        },

        error: (error) => {
          console.error(
            'Failed to load orders:',
            error
          );

          this.orders = [];
          this.loading = false;

          this.error =
            error?.error?.message ??
            'Unable to load your orders.';

          this.changeDetectorRef.detectChanges();
        }
      });
  }
}