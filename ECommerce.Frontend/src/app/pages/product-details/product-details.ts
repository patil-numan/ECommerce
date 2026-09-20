import {
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit
} from '@angular/core';

import { DecimalPipe } from '@angular/common';

import {
  ActivatedRoute,
  RouterLink
} from '@angular/router';

import { Subscription } from 'rxjs';

import { Navbar } from '../../components/navbar/navbar';

import { Product } from '../../models/product';
import { ProductService } from '../../services/product.service';
import { CartService } from '../../services/cart.service';

@Component({
  selector: 'app-product-details',
  imports: [
    DecimalPipe,
    RouterLink,
    Navbar
  ],
  templateUrl: './product-details.html',
  styleUrl: './product-details.css'
})
export class ProductDetails implements OnInit, OnDestroy {

  product: Product | null = null;

  loading = true;

  error = '';

  cartQuantity = 0;

  private cartSubscription?: Subscription;


  constructor(
    private readonly route: ActivatedRoute,
    private readonly productService: ProductService,
    private readonly cartService: CartService,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {}


  ngOnInit(): void {

    this.loadProduct();

    this.cartSubscription =
      this.cartService.cartItems$.subscribe(() => {

        if (this.product) {

          this.cartQuantity =
            this.cartService.getProductQuantity(
              this.product.id
            );

          this.changeDetectorRef.detectChanges();

        }

      });

  }


  ngOnDestroy(): void {

    this.cartSubscription?.unsubscribe();

  }


  // --------------------------------------------------
  // LOAD PRODUCT
  // --------------------------------------------------

  private loadProduct(): void {

    const id = Number(
      this.route.snapshot.paramMap.get('id')
    );

    console.log(
      'Product ID from route:',
      id
    );


    if (!id) {

      this.error =
        'Invalid product ID.';

      this.loading = false;

      this.changeDetectorRef.detectChanges();

      return;
    }


    this.productService
      .getProductById(id)
      .subscribe({

        next: (product: Product) => {

          console.log(
            'Product received:',
            product
          );

          this.product = product;

          this.cartQuantity =
            this.cartService.getProductQuantity(
              product.id
            );

          this.loading = false;

          this.changeDetectorRef.detectChanges();

        },


        error: (error) => {

          console.error(
            'Failed to load product:',
            error
          );

          this.product = null;

          this.error =
            'Unable to load this product.';

          this.loading = false;

          this.changeDetectorRef.detectChanges();

        }

      });

  }


  // --------------------------------------------------
  // ADD TO CART
  // --------------------------------------------------

  addToCart(): void {

    if (!this.product) {
      return;
    }


    if (
      this.cartQuantity >=
      this.product.stockQuantity
    ) {

      console.log(
        'Stock limit reached:',
        this.product.name
      );

      return;
    }


    this.cartService.addToCart(
      this.product
    );


    this.cartQuantity =
      this.cartService.getProductQuantity(
        this.product.id
      );


    console.log(
      'Added to cart:',
      this.product.name
    );

    console.log(
      'Current quantity:',
      this.cartQuantity
    );


    this.changeDetectorRef.detectChanges();

  }


  // --------------------------------------------------
  // CHECK STOCK LIMIT
  // --------------------------------------------------

  isStockLimitReached(): boolean {

    if (!this.product) {
      return false;
    }

    return (
      this.cartQuantity >=
      this.product.stockQuantity
    );

  }

}