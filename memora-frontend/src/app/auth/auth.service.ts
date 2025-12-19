import { Injectable } from '@angular/core';
import { Router } from '@angular/router';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private isLoggedIn = false;

  constructor(private router: Router) {}

  login(email: string, password: string) {
    if (email && password) {
      this.isLoggedIn = true;
      this.router.navigate(['/home']);
    }
  }

  register(email: string, password: string) {
    if (email && password) {
      this.isLoggedIn = true;
      this.router.navigate(['/home']);
    }
  }

  logout() {
    this.isLoggedIn = false;
    this.router.navigate(['/login']);
  }
}
