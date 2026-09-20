import {
  ChangeDetectorRef,
  Component,
  OnInit
} from '@angular/core';

import {
  DecimalPipe,
  DatePipe
} from '@angular/common';

import {
  ActivatedRoute,
  RouterLink
} from '@angular/router';

import { Order } from '../../models/order';
import { OrderService } from '../../services/order.service';

@Component({
  selector: 'app-order-confirmation',
  imports: [
    DecimalPipe,
    DatePipe,
    RouterLink
  ],
  templateUrl: './order-confirmation.html',
  styleUrl: './order-confirmation.css'
})
export class OrderConfirmation implements OnInit {
  order: Order | null = null;
  loading = true;
  error = '';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly orderService: OrderService,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadOrder();
  }

  private loadOrder(): void {
    const id = Number(
      this.route.snapshot.paramMap.get('id')
    );

    console.log('Order ID from route:', id);

    if (!id) {
      this.error = 'Invalid order ID.';
      this.loading = false;
      this.changeDetectorRef.detectChanges();
      return;
    }

    this.orderService.getOrderById(id).subscribe({
      next: (order: Order) => {
        console.log('Order received:', order);

        this.order = order;
        this.loading = false;

        this.changeDetectorRef.detectChanges();
      },

      error: (error) => {
        console.error(
          'Failed to load order:',
          error
        );

        this.order = null;
        this.loading = false;

        this.error =
          error?.error?.message ??
          'Unable to load your order.';

        this.changeDetectorRef.detectChanges();
      }
    });
  }
}