import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Subject, tap } from 'rxjs';
import { I18nService } from '../i18n.service';
import { environment } from '../../environment';

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
  private readonly apiUrl = environment.apiUrl + '/api/auth';
  private readonly accountUrl = environment.apiUrl + '/api/account';
  private loggedInSubject = new BehaviorSubject<boolean>(!!this.token);
  loggedIn$ = this.loggedInSubject.asObservable();
  private profileChangedSubject = new Subject<void>();
  profileChanged$ = this.profileChangedSubject.asObservable();

  constructor(private http: HttpClient, private i18n: I18nService) {}

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
    return confirm(this.i18n.translate('sidebar.logoutConfirm'));
  }

  notifyProfileChanged() {
    this.profileChangedSubject.next();
  }

  get token(): string | null {
    return localStorage.getItem('token');
  }

  isLoggedIn(): boolean {
    return !!this.token;
  }
}
