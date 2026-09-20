import {
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit
} from '@angular/core';

import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';

import { Navbar } from '../../components/navbar/navbar';

import { CartItem } from '../../models/cart-item';
import { CartService } from '../../services/cart.service';

@Component({
  selector: 'app-cart',
  imports: [
    DecimalPipe,
    RouterLink,
    Navbar
  ],
  templateUrl: './cart.html',
  styleUrl: './cart.css'
})
export class Cart implements OnInit, OnDestroy {

  cartItems: CartItem[] = [];

  total = 0;

  totalQuantity = 0;

  private cartSubscription?: Subscription;


  constructor(
    private readonly cartService: CartService,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {}


  // --------------------------------------------------
  // INITIALIZE
  // --------------------------------------------------

  ngOnInit(): void {

    console.log(
      'Cart page initialized'
    );

    this.cartSubscription =
      this.cartService.cartItems$.subscribe({

        next: (items: CartItem[]) => {

          console.log(
            'Cart items received:',
            items
          );

          this.cartItems = items;

          this.total =
            this.cartService.getTotal();

          this.totalQuantity =
            this.cartService.getItemCount();

          console.log(
            'Cart total:',
            this.total
          );

          console.log(
            'Cart quantity:',
            this.totalQuantity
          );

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
  // INCREASE QUANTITY
  // --------------------------------------------------

  increaseQuantity(
    productId: number
  ): void {

    console.log(
      'Increasing quantity:',
      productId
    );

    this.cartService.increaseQuantity(
      productId
    );

  }


  // --------------------------------------------------
  // DECREASE QUANTITY
  // --------------------------------------------------

  decreaseQuantity(
    productId: number
  ): void {

    console.log(
      'Decreasing quantity:',
      productId
    );

    this.cartService.decreaseQuantity(
      productId
    );

  }


  // --------------------------------------------------
  // REMOVE ITEM
  // --------------------------------------------------

  removeItem(
    productId: number
  ): void {

    console.log(
      'Removing product:',
      productId
    );

    this.cartService.removeFromCart(
      productId
    );

  }


  // --------------------------------------------------
  // CLEAR CART
  // --------------------------------------------------

  clearCart(): void {

    console.log(
      'Clearing cart'
    );

    this.cartService.clearCart();

  }


  // --------------------------------------------------
  // CHECK STOCK LIMIT
  // --------------------------------------------------

  isStockLimitReached(
    productId: number
  ): boolean {

    return this.cartService.isStockLimitReached(
      productId
    );

  }

}