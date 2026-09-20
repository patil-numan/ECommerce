import {
  ChangeDetectorRef,
  Component,
  OnInit
} from '@angular/core';

import {
  FormsModule
} from '@angular/forms';

import {
  ActivatedRoute,
  Router,
  RouterLink
} from '@angular/router';

import {
  Product,
  CreateProduct,
  UpdateProduct
} from '../../models/product';

import {
  ProductService
} from '../../services/product.service';

@Component({
  selector: 'app-admin-product-form',
  imports: [
    FormsModule,
    RouterLink
  ],
  templateUrl: './admin-product-form.html',
  styleUrl: './admin-product-form.css'
})
export class AdminProductForm implements OnInit {

  productId: number | null = null;

  isEditMode = false;

  loading = false;
  saving = false;

  error = '';
  success = '';

  sku = '';
  name = '';
  description = '';
  price = 0;
  stockQuantity = 0;
  categoryId = 0;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly productService: ProductService,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');

    if (id) {
      this.productId = Number(id);
      this.isEditMode = true;
      this.loadProduct();
      return;
    }

    this.isEditMode = false;
    this.loading = false;
  }

  private loadProduct(): void {
    if (!this.productId) {
      return;
    }

    this.loading = true;
    this.error = '';

    console.log(
      'Loading product:',
      this.productId
    );

    this.productService
      .getProductById(this.productId)
      .subscribe({
        next: (product: Product) => {
          console.log(
            'Product received:',
            product
          );

          this.sku = product.sku;
          this.name = product.name;
          this.description = product.description;
          this.price = product.price;
          this.stockQuantity =
            product.stockQuantity;
          this.categoryId =
            product.categoryId;

          this.loading = false;

          this.changeDetectorRef.detectChanges();
        },

        error: (error) => {
          console.error(
            'Failed to load product:',
            error
          );

          this.loading = false;

          this.error =
            error?.error?.message ??
            'Unable to load this product.';

          this.changeDetectorRef.detectChanges();
        }
      });
  }

  saveProduct(): void {
    this.error = '';
    this.success = '';

    if (
      !this.sku.trim() ||
      !this.name.trim() ||
      !this.description.trim()
    ) {
      this.error =
        'Please fill in all product fields.';

      return;
    }

    if (this.price < 0) {
      this.error =
        'Price cannot be negative.';

      return;
    }

    if (this.stockQuantity < 0) {
      this.error =
        'Stock quantity cannot be negative.';

      return;
    }

    if (this.categoryId <= 0) {
      this.error =
        'Please enter a valid category ID.';

      return;
    }

    this.saving = true;

    if (this.isEditMode && this.productId) {
      this.updateProduct();
      return;
    }

    this.createProduct();
  }

  private createProduct(): void {
    const product: CreateProduct = {
      sku: this.sku.trim(),
      name: this.name.trim(),
      description: this.description.trim(),
      price: this.price,
      categoryId: this.categoryId,
      stockQuantity: this.stockQuantity
    };

    console.log(
      'Creating product:',
      product
    );

    this.productService
      .createProduct(product)
      .subscribe({
        next: (createdProduct: Product) => {
          console.log(
            'Product created successfully:',
            createdProduct
          );

          this.saving = false;

          this.router.navigate([
            '/admin/products'
          ]);
        },

        error: (error) => {
          console.error(
            'Failed to create product:',
            error
          );

          this.saving = false;

          this.error =
            error?.error?.message ??
            'Unable to create the product.';

          this.changeDetectorRef.detectChanges();
        }
      });
  }

  private updateProduct(): void {
    if (!this.productId) {
      return;
    }

    const product: UpdateProduct = {
      sku: this.sku.trim(),
      name: this.name.trim(),
      description: this.description.trim(),
      price: this.price,
      stockQuantity: this.stockQuantity,
      categoryId: this.categoryId
    };

    console.log(
      'Updating product:',
      this.productId,
      product
    );

    this.productService
      .updateProduct(
        this.productId,
        product
      )
      .subscribe({
        next: (updatedProduct: Product) => {
          console.log(
            'Product updated successfully:',
            updatedProduct
          );

          this.saving = false;

          this.router.navigate([
            '/admin/products'
          ]);
        },

        error: (error) => {
          console.error(
            'Failed to update product:',
            error
          );

          this.saving = false;

          this.error =
            error?.error?.message ??
            'Unable to update the product.';

          this.changeDetectorRef.detectChanges();
        }
      });
  }
}