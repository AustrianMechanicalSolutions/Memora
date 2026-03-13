import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, tap } from 'rxjs';

interface AuthResponse {
  token: string;
}

export interface CurrentUser {
  id: string;
  email: string;
  displayName: string;
  bio?: string;
  status?: string;
  birthDate?: string;
  profileImageUrl?: string;
  phoneNumber?: string;
  discordTag?: string;
  instagramUrl?: string;
  tikTokUrl?: string;
  youTubeUrl?: string;
  websiteUrl?: string;
  twoFactorEnabled: boolean;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiUrl = 'http://localhost:5000/api/auth';
  private readonly accountUrl = 'http://localhost:5000/api/account';
  private loggedInSubject = new BehaviorSubject<boolean>(!!this.token);
  loggedIn$ = this.loggedInSubject.asObservable();

  constructor(private http: HttpClient) {}

  login(email: string, password: string, twoFactorCode?: string) {
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/login`, { email, password, twoFactorCode })
      .pipe(
        tap(res => {
          localStorage.setItem('token', res.token);
          this.loggedInSubject.next(true);
        })
      );
  }

  register(email: string, password: string) {
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/register`, { email, password })
      .pipe(
        tap(res => {
          localStorage.setItem('token', res.token);
          this.loggedInSubject.next(true);
        })
      );
  }

  currentUser() {
    return this.http.get<CurrentUser>(`${this.accountUrl}/me`);
  }

  logout() {
    localStorage.removeItem('token');
    this.loggedInSubject.next(false);
  }

  confirmLogout(): boolean {
    return confirm('Are you sure you want to logout?');
  }

  get token(): string | null {
    return localStorage.getItem('token');
  }

  isLoggedIn(): boolean {
    return !!this.token;
  }
}
