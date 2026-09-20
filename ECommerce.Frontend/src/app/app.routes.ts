import { Routes } from '@angular/router';

import { authGuard } from './guards/auth.guard';
import { adminGuard } from './guards/admin.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/home/home')
        .then(m => m.Home)
  },

  {
    path: 'login',
    loadComponent: () =>
      import('./pages/login/login')
        .then(m => m.Login)
  },

  {
    path: 'register',
    loadComponent: () =>
      import('./pages/register/register')
        .then(m => m.Register)
  },

  {
    path: 'products/:id',
    loadComponent: () =>
      import('./pages/product-details/product-details')
        .then(m => m.ProductDetails)
  },

  {
    path: 'cart',
    loadComponent: () =>
      import('./pages/cart/cart')
        .then(m => m.Cart)
  },

  {
    path: 'checkout',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./pages/checkout/checkout')
        .then(m => m.Checkout)
  },

  {
    path: 'orders',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./pages/orders/orders')
        .then(m => m.Orders)
  },

  {
    path: 'orders/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./pages/order-confirmation/order-confirmation')
        .then(m => m.OrderConfirmation)
  },

  {
    path: 'admin',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('./pages/admin/admin')
        .then(m => m.Admin)
  },

  {
  path: 'admin/products',
  canActivate: [adminGuard],
  loadComponent: () =>
    import('./pages/admin-products/admin-products')
      .then(m => m.AdminProducts)
  },

  {
  path: 'admin/products/new',
  canActivate: [adminGuard],
  loadComponent: () =>
    import('./pages/admin-product-form/admin-product-form')
      .then(m => m.AdminProductForm)
},

{
  path: 'admin/products/:id',
  canActivate: [adminGuard],
  loadComponent: () =>
    import('./pages/admin-product-form/admin-product-form')
      .then(m => m.AdminProductForm)
},

  {
    path: 'admin/orders/:id',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('./pages/admin-order-details/admin-order-details')
        .then(m => m.AdminOrderDetails)
  },

  {
    path: '**',
    redirectTo: ''
  }
];