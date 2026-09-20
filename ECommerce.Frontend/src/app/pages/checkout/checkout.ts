import {
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit
} from '@angular/core';

import { DecimalPipe } from '@angular/common';
import {
  Router,
  RouterLink
} from '@angular/router';

import { Subscription } from 'rxjs';

import { Navbar } from '../../components/navbar/navbar';

import { CartItem } from '../../models/cart-item';
import { Order } from '../../models/order';

import { CartService } from '../../services/cart.service';
import { OrderService } from '../../services/order.service';


@Component({
  selector: 'app-checkout',
  imports: [
    DecimalPipe,
    RouterLink,
    Navbar
  ],
  templateUrl: './checkout.html',
  styleUrl: './checkout.css'
})
export class Checkout implements OnInit, OnDestroy {

  cartItems: CartItem[] = [];

  total = 0;

  totalQuantity = 0;

  loading = false;

  error = '';

  private cartSubscription?: Subscription;


  constructor(
    private readonly cartService: CartService,
    private readonly orderService: OrderService,
    private readonly router: Router,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {}


  // --------------------------------------------------
  // INITIALIZE
  // --------------------------------------------------

  ngOnInit(): void {

    this.cartSubscription =
      this.cartService.cartItems$.subscribe({

        next: (items: CartItem[]) => {

          this.cartItems = items;

          this.total =
            this.cartService.getTotal();

          this.totalQuantity =
            this.cartService.getItemCount();

          this.changeDetectorRef.detectChanges();

        }

      });

  }


  // --------------------------------------------------
  // CLEANUP
  // --------------------------------------------------

  ngOnDestroy(): void {

    this.cartSubscription?.unsubscribe();

  }


  // --------------------------------------------------
  // PLACE ORDER
  // --------------------------------------------------

  placeOrder(): void {

    this.error = '';


    // Prevent checkout with an empty cart

    if (this.cartItems.length === 0) {

      this.error =
        'Your cart is empty.';

      return;

    }


    this.loading = true;


    // Convert CartItem[] into CreateOrder format

    const orderRequest = {

      items: this.cartItems.map(
        item => ({
          productId:
            item.product.id,

          quantity:
            item.quantity
        })
      )

    };


    console.log(
      'Creating order:',
      orderRequest
    );


    this.orderService
      .createOrder(orderRequest)
      .subscribe({

        next: (order: Order) => {

          console.log(
            'Order created successfully:',
            order
          );


          // Clear cart after successful order

          this.cartService.clearCart();


          this.loading = false;

          this.changeDetectorRef.detectChanges();


          // Navigate to order confirmation

          this.router.navigate([
            '/orders',
            order.id
          ]);

        },


        error: (error) => {

          console.error(
            'Order creation failed:',
            error
          );


          this.loading = false;


          this.error =
            error?.error?.message ??
            'Unable to place the order. Please try again.';


          this.changeDetectorRef.detectChanges();

        }

      });

  }

}