import { inject } from '@angular/core';
import {
  CanActivateFn,
  Router
} from '@angular/router';

import { AuthService } from '../services/auth.service';

export const adminGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const loggedIn = authService.isLoggedIn();
  const admin = authService.isAdmin();

  console.log('Admin Guard Check');
  console.log('Logged in:', loggedIn);
  console.log('Is admin:', admin);

  if (!loggedIn) {
    console.log(
      'Admin access denied: user is not logged in.'
    );

    return router.createUrlTree(['/login']);
  }

  if (!admin) {
    console.log(
      'Admin access denied: user is not an admin.'
    );

    return router.createUrlTree(['/']);
  }

  console.log(
    'Admin access granted.'
  );

  return true;
};