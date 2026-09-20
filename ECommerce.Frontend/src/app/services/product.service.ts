import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import {
  Product,
  CreateProduct,
  UpdateProduct
} from '../models/product';

import {
  ImportJobResponse,
  ImportJobStatus
} from '../models/import-job';

@Injectable({
  providedIn: 'root'
})
export class ProductService {
  private readonly apiUrl = 'https://localhost:7165/api/Products';

  constructor(private readonly http: HttpClient) {}

  getProducts(): Observable<Product[]> {
    return this.http.get<Product[]>(this.apiUrl);
  }

  getProductById(id: number): Observable<Product> {
    return this.http.get<Product>(`${this.apiUrl}/${id}`);
  }

  createProduct(product: CreateProduct): Observable<Product> {
    return this.http.post<Product>(this.apiUrl, product);
  }

  updateProduct(
    id: number,
    product: UpdateProduct
  ): Observable<Product> {
    return this.http.put<Product>(
      `${this.apiUrl}/${id}`,
      product
    );
  }

  deleteProduct(id: number): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/${id}`
    );
  }

  uploadBulkProducts(file: File): Observable<ImportJobResponse> {
    const formData = new FormData();

    formData.append('file', file);

    return this.http.post<ImportJobResponse>(
      `${this.apiUrl}/bulk-update`,
      formData
    );
  }

  getImportJobStatus(jobId: number): Observable<ImportJobStatus> {
    return this.http.get<ImportJobStatus>(
      `${this.apiUrl}/import-jobs/${jobId}`
    );
  }
}