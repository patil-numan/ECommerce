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
  FormsModule
} from '@angular/forms';

import {
  ActivatedRoute,
  RouterLink
} from '@angular/router';

import { Order } from '../../models/order';
import { OrderService } from '../../services/order.service';

@Component({
  selector: 'app-admin-order-details',
  imports: [
    DecimalPipe,
    DatePipe,
    FormsModule,
    RouterLink
  ],
  templateUrl: './admin-order-details.html',
  styleUrl: './admin-order-details.css'
})
export class AdminOrderDetails implements OnInit {
  order: Order | null = null;

  loading = true;
  updating = false;
  error = '';
  success = '';

  selectedStatus = '';

  readonly statuses = [
    'Pending',
    'Paid',
    'Shipped',
    'Delivered',
    'Cancelled'
  ];

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

    console.log(
      'Admin order ID from route:',
      id
    );

    if (!id) {
      this.error = 'Invalid order ID.';
      this.loading = false;
      this.changeDetectorRef.detectChanges();
      return;
    }

    this.orderService
      .getAdminOrderById(id)
      .subscribe({
        next: (order: Order) => {
          console.log(
            'Admin order received:',
            order
          );

          this.order = order;
          this.selectedStatus = order.status;
          this.loading = false;

          this.changeDetectorRef.detectChanges();
        },

        error: (error) => {
          console.error(
            'Failed to load admin order:',
            error
          );

          this.order = null;
          this.loading = false;

          this.error =
            error?.error?.message ??
            'Unable to load this order.';

          this.changeDetectorRef.detectChanges();
        }
      });
  }

  updateStatus(): void {
    if (!this.order) {
      return;
    }

    if (
      !this.selectedStatus ||
      this.selectedStatus === this.order.status
    ) {
      return;
    }

    this.updating = true;
    this.error = '';
    this.success = '';

    console.log(
      'Updating order status:',
      this.order.id,
      this.selectedStatus
    );

    this.orderService
      .updateOrderStatus(
        this.order.id,
        this.selectedStatus
      )
      .subscribe({
        next: (updatedOrder: Order) => {
          console.log(
            'Order status updated:',
            updatedOrder
          );

          this.order = updatedOrder;
          this.selectedStatus =
            updatedOrder.status;

          this.updating = false;
          this.success =
            'Order status updated successfully.';

          this.changeDetectorRef.detectChanges();
        },

        error: (error) => {
          console.error(
            'Failed to update order status:',
            error
          );

          this.updating = false;

          this.error =
            error?.error?.message ??
            'Unable to update the order status.';

          this.changeDetectorRef.detectChanges();
        }
      });
  }
}