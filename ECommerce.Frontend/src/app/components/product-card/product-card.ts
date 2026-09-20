import { Component, Input } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';

import { Product } from '../../models/product';

@Component({
  selector: 'app-product-card',
  imports: [
    DecimalPipe,
    RouterLink
  ],
  templateUrl: './product-card.html',
  styleUrl: './product-card.css'
})
export class ProductCard {

  @Input()
  product!: Product;

}