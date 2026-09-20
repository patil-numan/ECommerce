import {
  ChangeDetectorRef,
  Component,
  OnInit
} from '@angular/core';

import {
  DatePipe,
  DecimalPipe
} from '@angular/common';

import {
  RouterLink
} from '@angular/router';

import { Navbar } from '../../components/navbar/navbar';

import { Order } from '../../models/order';
import { OrderService } from '../../services/order.service';

@Component({
  selector: 'app-admin',
  imports: [
    DatePipe,
    DecimalPipe,
    RouterLink,
    Navbar
  ],
  templateUrl: './admin.html',
  styleUrl: './admin.css'
})
export class Admin implements OnInit {
  orders: Order[] = [];

  loading = true;
  error = '';

  constructor(
    private readonly orderService: OrderService,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadOrders();
  }

  private loadOrders(): void {
    this.loading = true;
    this.error = '';

    console.log('Loading admin orders...');

    this.orderService.getAdminOrders().subscribe({
      next: (orders: Order[]) => {
        console.log(
          'Admin orders received:',
          orders
        );

        this.orders = orders;
        this.loading = false;

        this.changeDetectorRef.detectChanges();
      },

      error: (error) => {
        console.error(
          'Failed to load admin orders:',
          error
        );

        this.orders = [];
        this.loading = false;

        this.error =
          error?.error?.message ??
          'Unable to load admin orders.';

        this.changeDetectorRef.detectChanges();
      }
    });
  }
}