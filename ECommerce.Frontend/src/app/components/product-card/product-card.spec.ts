import {
  ChangeDetectorRef,
  Component,
  OnInit
} from '@angular/core';

import {
  DecimalPipe
} from '@angular/common';

import {
  RouterLink
} from '@angular/router';

import { Navbar } from '../../components/navbar/navbar';

import { Product } from '../../models/product';
import { ProductService } from '../../services/product.service';

@Component({
  selector: 'app-admin-products',
  imports: [
    DecimalPipe,
    RouterLink,
    Navbar
  ],
  templateUrl: './admin-products.html',
  styleUrl: './admin-products.css'
})
export class AdminProducts implements OnInit {
  products: Product[] = [];

  loading = true;
  deletingProductId: number | null = null;

  error = '';

  constructor(
    private readonly productService: ProductService,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadProducts();
  }

  private loadProducts(): void {
    this.loading = true;
    this.error = '';

    console.log(
      'Loading admin products...'
    );

    this.productService
      .getProducts()
      .subscribe({
        next: (products: Product[]) => {
          console.log(
            'Admin products received:',
            products
          );

          this.products = products;
          this.loading = false;

          this.changeDetectorRef.detectChanges();
        },

        error: (error) => {
          console.error(
            'Failed to load admin products:',
            error
          );

          this.products = [];
          this.loading = false;

          this.error =
            error?.error?.message ??
            'Unable to load products.';

          this.changeDetectorRef.detectChanges();
        }
      });
  }

  deleteProduct(product: Product): void {
    const confirmed = window.confirm(
      `Are you sure you want to delete "${product.name}"?`
    );

    if (!confirmed) {
      return;
    }

    this.deletingProductId = product.id;
    this.error = '';

    console.log(
      'Deleting product:',
      product.id,
      product.name
    );

    this.productService
      .deleteProduct(product.id)
      .subscribe({
        next: () => {
          console.log(
            'Product deleted successfully:',
            product
          );

          this.products =
            this.products.filter(
              currentProduct =>
                currentProduct.id !== product.id
            );

          this.deletingProductId = null;

          this.changeDetectorRef.detectChanges();
        },

        error: (error) => {
          console.error(
            'Failed to delete product:',
            error
          );

          this.deletingProductId = null;

          this.error =
            error?.error?.message ??
            'Unable to delete the product.';

          this.changeDetectorRef.detectChanges();
        }
      });
  }
}