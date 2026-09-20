import {
  ChangeDetectorRef,
  Component,
  OnInit
} from '@angular/core';

import { FormsModule } from '@angular/forms';

import { Navbar } from '../../components/navbar/navbar';
import { ProductCard } from '../../components/product-card/product-card';

import { Product } from '../../models/product';
import { ProductService } from '../../services/product.service';

@Component({
  selector: 'app-home',
  imports: [
    FormsModule,
    Navbar,
    ProductCard
  ],
  templateUrl: './home.html',
  styleUrl: './home.css'
})
export class Home implements OnInit {

  products: Product[] = [];

  loading = true;

  error = '';

  searchTerm = '';

  appliedSearchTerm = '';

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

    console.log('Loading products from Home...');

    this.productService.getProducts().subscribe({

      next: (products: Product[]) => {

        console.log(
          'Products received in Home:',
          products
        );

        console.log(
          'Product count:',
          products.length
        );

        this.products = products;

        this.loading = false;

        console.log(
          'Home loading state:',
          this.loading
        );

        this.changeDetectorRef.detectChanges();

      },

      error: (error) => {

        console.error(
          'Failed to load products:',
          error
        );

        this.products = [];

        this.error =
          'Unable to load products. Please try again.';

        this.loading = false;

        this.changeDetectorRef.detectChanges();

      }

    });
  }

  searchProducts(): void {

    this.appliedSearchTerm =
      this.searchTerm.trim().toLowerCase();

  }

  get filteredProducts(): Product[] {

    if (!this.appliedSearchTerm) {
      return this.products;
    }

    return this.products.filter(product =>
      product.name
        .toLowerCase()
        .includes(this.appliedSearchTerm) ||

      product.sku
        .toLowerCase()
        .includes(this.appliedSearchTerm)
    );

  }
}