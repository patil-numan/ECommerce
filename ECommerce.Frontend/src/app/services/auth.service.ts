import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { jwtDecode } from 'jwt-decode';

import { LoginRequest } from '../models/login';
import { AuthResponse } from '../models/auth-response';
import { RegisterRequest } from '../models/register';

interface JwtPayload {
  role?: string;

  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'?: string;

  [key: string]: unknown;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiUrl =
    'https://localhost:7165/api/Auth';

  private readonly tokenKey =
    'ecommerce_token';

  constructor(
    private readonly http: HttpClient
  ) {}

  login(
    credentials: LoginRequest
  ): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(
        `${this.apiUrl}/login`,
        credentials
      )
      .pipe(
        tap((response: AuthResponse) => {
          localStorage.setItem(
            this.tokenKey,
            response.token
          );
        })
      );
  }

  register(
    request: RegisterRequest
  ): Observable<unknown> {
    return this.http.post(
      `${this.apiUrl}/register`,
      request
    );
  }

  logout(): void {
    localStorage.removeItem(
      this.tokenKey
    );
  }

  getToken(): string | null {
    return localStorage.getItem(
      this.tokenKey
    );
  }

  isLoggedIn(): boolean {
    return this.getToken() !== null;
  }

  isAdmin(): boolean {
    const token = this.getToken();

    if (!token) {
      return false;
    }

    try {
      const payload =
        jwtDecode<JwtPayload>(token);

      const role =
        payload.role ??
        payload[
          'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'
        ];

      console.log(
        'JWT role:',
        role
      );

      return role === 'Admin';
    } catch (error) {
      console.error(
        'Failed to decode JWT:',
        error
      );

      return false;
    }
  }
}