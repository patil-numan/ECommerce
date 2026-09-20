import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Navbar } from '../../components/navbar/navbar';
import { Product } from '../../models/product';
import { ProductService } from '../../services/product.service';

@Component({
  selector: 'app-admin-products',
  imports: [
    DecimalPipe,
    FormsModule,
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

  searchTerm = '';
  appliedSearchTerm = '';

  selectedFile: File | null = null;
  uploading = false;

  importJobId: number | null = null;
  importStatus = '';
  importTotalRows = 0;
  importProcessedRows = 0;
  importUpdatedRows = 0;
  importCreatedRows = 0;
  importFailedRows = 0;
  importErrorMessage: string | null = null;

  private importPollingTimer: ReturnType<typeof setTimeout> | null = null;

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

    console.log('Loading admin products...');

    this.productService.getProducts().subscribe({
      next: (products: Product[]) => {
        console.log('Admin products received:', products);

        this.products = products;
        this.loading = false;

        this.changeDetectorRef.detectChanges();
      },

      error: (error) => {
        console.error('Failed to load admin products:', error);

        this.products = [];
        this.loading = false;

        this.error =
          error?.error?.message ??
          'Unable to load products.';

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

    this.productService.deleteProduct(product.id).subscribe({
      next: () => {
        console.log(
          'Product deleted successfully:',
          product
        );

        this.products = this.products.filter(
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

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;

    if (!input.files || input.files.length === 0) {
      this.selectedFile = null;
      return;
    }

    const file = input.files[0];

    if (!file.name.toLowerCase().endsWith('.xlsx')) {
      this.selectedFile = null;

      this.error =
        'Only .xlsx Excel files are supported.';

      input.value = '';

      return;
    }

    this.error = '';
    this.selectedFile = file;

    console.log(
      'Excel file selected:',
      file.name
    );
  }

  uploadExcel(): void {
    if (!this.selectedFile) {
      this.error = 'Please select an Excel file first.';
      return;
    }

    this.uploading = true;
    this.error = '';
    this.importErrorMessage = null;

    console.log(
      'Uploading Excel file:',
      this.selectedFile.name
    );

    this.productService
      .uploadBulkProducts(this.selectedFile)
      .subscribe({
        next: (response) => {
          console.log(
            'Import job created:',
            response
          );

          this.uploading = false;

          this.importJobId = response.jobId;
          this.importStatus = 'Pending';

          this.importTotalRows = 0;
          this.importProcessedRows = 0;
          this.importUpdatedRows = 0;
          this.importCreatedRows = 0;
          this.importFailedRows = 0;

          this.selectedFile = null;

          this.startImportPolling();

          this.changeDetectorRef.detectChanges();
        },

        error: (error) => {
          console.error(
            'Excel upload failed:',
            error
          );

          this.uploading = false;

          this.error =
            error?.error?.message ??
            'Unable to upload the Excel file.';

          this.changeDetectorRef.detectChanges();
        }
      });
  }

  private startImportPolling(): void {
    if (this.importPollingTimer) {
      clearTimeout(this.importPollingTimer);
    }

    this.checkImportJobStatus();
  }

  private checkImportJobStatus(): void {
    if (this.importJobId === null) {
      return;
    }

    this.productService
      .getImportJobStatus(this.importJobId)
      .subscribe({
        next: (job) => {
          this.importStatus = job.status;
          this.importTotalRows = job.totalRows;
          this.importProcessedRows = job.processedRows;
          this.importUpdatedRows = job.updatedRows;
          this.importCreatedRows = job.createdRows;
          this.importFailedRows = job.failedRows;
          this.importErrorMessage = job.errorMessage;

          console.log(
            'Import job status:',
            job
          );

          this.changeDetectorRef.detectChanges();

          const finished =
            job.status === 'Completed' ||
            job.status === 'Failed';

          if (finished) {
            if (job.status === 'Completed') {
              this.loadProducts();
            }

            return;
          }

          this.importPollingTimer = setTimeout(
            () => this.checkImportJobStatus(),
            2000
          );
        },

        error: (error) => {
          console.error(
            'Failed to get import job status:',
            error
          );

          this.importErrorMessage =
            error?.error?.message ??
            'Unable to check import status.';

          this.changeDetectorRef.detectChanges();
        }
      });
  }

  getImportProgress(): number {
    if (this.importTotalRows <= 0) {
      return 0;
    }

    return Math.round(
      (this.importProcessedRows /
        this.importTotalRows) *
        100
    );
  }
}