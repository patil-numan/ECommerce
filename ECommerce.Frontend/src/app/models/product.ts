export interface Product {
  id: number;
  sku: string;
  name: string;
  description: string;
  price: number;
  categoryId: number;
  stockQuantity: number;
}

export interface CreateProduct {
  sku: string;
  name: string;
  description: string;
  price: number;
  categoryId: number;
  stockQuantity: number;
}

export interface UpdateProduct {
  sku: string;
  name: string;
  description: string;
  price: number;
  stockQuantity: number;
  categoryId: number;
}