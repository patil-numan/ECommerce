import {
  ChangeDetectorRef,
  Component
} from '@angular/core';

import {
  FormsModule
} from '@angular/forms';

import {
  Router,
  RouterLink
} from '@angular/router';

import {
  AuthService
} from '../../services/auth.service';

@Component({
  selector: 'app-register',
  imports: [
    FormsModule,
    RouterLink
  ],
  templateUrl: './register.html',
  styleUrl: './register.css'
})
export class Register {
  email = '';
  password = '';
  confirmPassword = '';

  loading = false;
  error = '';
  success = '';

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {}

  register(): void {
    this.error = '';
    this.success = '';

    if (
      !this.email.trim() ||
      !this.password ||
      !this.confirmPassword
    ) {
      this.error =
        'Please fill in all fields.';
      return;
    }

    if (
      this.password !==
      this.confirmPassword
    ) {
      this.error =
        'Passwords do not match.';
      return;
    }

    this.loading = true;

    this.authService.register({
      email: this.email.trim(),
      password: this.password,
      confirmPassword: this.confirmPassword
    }).subscribe({
      next: () => {
        console.log(
          'Registration successful.'
        );

        this.loading = false;

        this.success =
          'Registration successful. Redirecting to login...';

        this.changeDetectorRef.detectChanges();

        setTimeout(() => {
          this.router.navigate(['/login']);
        }, 1000);
      },

      error: (error) => {
        console.error(
          'Registration failed:',
          error
        );

        this.loading = false;

        this.error =
          error?.error?.message ??
          'Registration failed. Please try again.';

        this.changeDetectorRef.detectChanges();
      }
    });
  }
}