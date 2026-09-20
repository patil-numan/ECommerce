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
  selector: 'app-login',
  imports: [
    FormsModule,
    RouterLink
  ],
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class Login {

  email = '';

  password = '';

  loading = false;

  error = '';


  constructor(
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {}


  login(): void {

    this.error = '';


    if (
      !this.email.trim() ||
      !this.password
    ) {

      this.error =
        'Please enter your email and password.';

      return;
    }


    this.loading = true;


    this.authService
      .login({
        email: this.email.trim(),
        password: this.password
      })
      .subscribe({

        next: () => {

          console.log(
            'Login successful.'
          );

          this.loading = false;

          this.changeDetectorRef.detectChanges();

          this.router.navigate([
            '/'
          ]);

        },


        error: (error) => {

          console.error(
            'Login failed:',
            error
          );

          this.loading = false;

          this.error =
            error?.error?.message ??
            'Invalid email or password.';

          this.changeDetectorRef.detectChanges();

        }

      });

  }

}