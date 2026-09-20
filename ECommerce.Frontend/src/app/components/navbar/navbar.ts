import {
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit
} from '@angular/core';

import { Router, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';

import { CartService } from '../../services/cart.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-navbar',
  imports: [RouterLink],
  templateUrl: './navbar.html',
  styleUrl: './navbar.css'
})
export class Navbar implements OnInit, OnDestroy {
  cartItemCount = 0;
  isLoggedIn = false;

  private cartSubscription?: Subscription;

  constructor(
    private readonly cartService: CartService,
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.isLoggedIn =
      this.authService.isLoggedIn();

    this.cartSubscription =
      this.cartService.cartItems$.subscribe(() => {
        this.cartItemCount =
          this.cartService.getItemCount();

        this.changeDetectorRef.detectChanges();
      });
  }

  ngOnDestroy(): void {
    this.cartSubscription?.unsubscribe();
  }

  logout(): void {
    this.authService.logout();

    this.isLoggedIn = false;

    console.log('User logged out.');

    this.changeDetectorRef.detectChanges();

    this.router.navigate(['/']);
  }
}