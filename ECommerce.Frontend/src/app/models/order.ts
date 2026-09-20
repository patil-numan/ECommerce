export interface OrderItem {
  productId: number;
  productName: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
}

export interface Order {
  id: number;
  userId: string;
  orderDate: string;
  totalAmount: number;
  status: string;
  items: OrderItem[];
}