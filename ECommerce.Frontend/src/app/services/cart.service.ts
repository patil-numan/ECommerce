import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

import { Product } from '../models/product';
import { CartItem } from '../models/cart-item';

@Injectable({
  providedIn: 'root'
})
export class CartService {

  private readonly storageKey = 'ecommerce_cart';

  private readonly cartItemsSubject =
    new BehaviorSubject<CartItem[]>(
      this.loadCart()
    );

  readonly cartItems$: Observable<CartItem[]> =
    this.cartItemsSubject.asObservable();


  // --------------------------------------------------
  // ADD PRODUCT TO CART
  // --------------------------------------------------

  addToCart(product: Product): void {

    const currentItems =
      this.cartItemsSubject.value;

    const existingItem =
      currentItems.find(
        item => item.product.id === product.id
      );


    // Product already exists in cart
    if (existingItem) {

      // Do not allow quantity above stock
      if (
        existingItem.quantity >=
        product.stockQuantity
      ) {

        console.log(
          'Cannot add more. Stock limit reached:',
          product.name
        );

        return;
      }


      const updatedItems =
        currentItems.map(item =>
          item.product.id === product.id
            ? {
                ...item,
                quantity: item.quantity + 1
              }
            : item
        );

      this.updateCart(updatedItems);

      return;
    }


    // Product is being added for the first time
    if (product.stockQuantity <= 0) {

      console.log(
        'Cannot add product. Product is out of stock:',
        product.name
      );

      return;
    }


    const newItem: CartItem = {
      product,
      quantity: 1
    };

    const updatedItems = [
      ...currentItems,
      newItem
    ];

    this.updateCart(updatedItems);
  }


  // --------------------------------------------------
  // INCREASE QUANTITY
  // --------------------------------------------------

  increaseQuantity(productId: number): void {

    const currentItems =
      this.cartItemsSubject.value;

    const updatedItems =
      currentItems.map(item => {

        if (item.product.id !== productId) {
          return item;
        }


        // Stock limit reached
        if (
          item.quantity >=
          item.product.stockQuantity
        ) {

          console.log(
            'Cannot increase quantity. Stock limit reached:',
            item.product.name
          );

          return item;
        }


        return {
          ...item,
          quantity: item.quantity + 1
        };

      });

    this.updateCart(updatedItems);
  }


  // --------------------------------------------------
  // DECREASE QUANTITY
  // --------------------------------------------------

  decreaseQuantity(productId: number): void {

    const updatedItems =
      this.cartItemsSubject.value
        .map(item =>
          item.product.id === productId
            ? {
                ...item,
                quantity: item.quantity - 1
              }
            : item
        )
        .filter(
          item => item.quantity > 0
        );

    this.updateCart(updatedItems);
  }


  // --------------------------------------------------
  // REMOVE PRODUCT
  // --------------------------------------------------

  removeFromCart(productId: number): void {

    const updatedItems =
      this.cartItemsSubject.value.filter(
        item =>
          item.product.id !== productId
      );

    this.updateCart(updatedItems);
  }


  // --------------------------------------------------
  // CLEAR CART
  // --------------------------------------------------

  clearCart(): void {

    this.updateCart([]);
  }


  // --------------------------------------------------
  // GET TOTAL PRICE
  // --------------------------------------------------

  getTotal(): number {

    return this.cartItemsSubject.value.reduce(
      (total, item) =>
        total +
        item.product.price *
        item.quantity,
      0
    );
  }


  // --------------------------------------------------
  // GET TOTAL QUANTITY
  // --------------------------------------------------

  getItemCount(): number {

    return this.cartItemsSubject.value.reduce(
      (count, item) =>
        count + item.quantity,
      0
    );
  }


  // --------------------------------------------------
  // GET QUANTITY OF ONE PRODUCT
  // --------------------------------------------------

  getProductQuantity(productId: number): number {

    const item =
      this.cartItemsSubject.value.find(
        item =>
          item.product.id === productId
      );

    return item?.quantity ?? 0;
  }


  // --------------------------------------------------
  // CHECK WHETHER STOCK LIMIT IS REACHED
  // --------------------------------------------------

  isStockLimitReached(productId: number): boolean {

    const item =
      this.cartItemsSubject.value.find(
        item =>
          item.product.id === productId
      );

    if (!item) {
      return false;
    }

    return (
      item.quantity >=
      item.product.stockQuantity
    );
  }


  // --------------------------------------------------
  // UPDATE CART
  // --------------------------------------------------

  private updateCart(
    items: CartItem[]
  ): void {

    this.cartItemsSubject.next(items);

    localStorage.setItem(
      this.storageKey,
      JSON.stringify(items)
    );

    console.log(
      'Cart state:',
      items
    );
  }


  // --------------------------------------------------
  // LOAD CART FROM LOCAL STORAGE
  // --------------------------------------------------

  private loadCart(): CartItem[] {

    const storedCart =
      localStorage.getItem(
        this.storageKey
      );

    if (!storedCart) {
      return [];
    }

    try {

      const parsedCart =
        JSON.parse(storedCart);

      if (!Array.isArray(parsedCart)) {
        return [];
      }

      return parsedCart;

    } catch (error) {

      console.error(
        'Failed to load cart:',
        error
      );

      return [];
    }
  }
}