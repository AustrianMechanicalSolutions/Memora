import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

type ThemeMode = 'dark' | 'light';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly storageKey = 'theme';
  private readonly themeSubject = new BehaviorSubject<ThemeMode>('dark');
  readonly theme$ = this.themeSubject.asObservable();

  constructor() {
    const initial = this.readStoredTheme();
    this.applyTheme(initial);
    this.themeSubject.next(initial);
  }

  get current(): ThemeMode {
    return this.themeSubject.value;
  }

  setTheme(mode: ThemeMode) {
    this.applyTheme(mode);
    this.themeSubject.next(mode);
    localStorage.setItem(this.storageKey, mode);
  }

  toggleTheme() {
    this.setTheme(this.current === 'dark' ? 'light' : 'dark');
  }

  private readStoredTheme(): ThemeMode {
    const stored = localStorage.getItem(this.storageKey);
    if (stored === 'light' || stored === 'dark') {
      return stored;
    }
    const prefersLight = window.matchMedia?.('(prefers-color-scheme: light)').matches ?? false;
    return prefersLight ? 'light' : 'dark';
  }

  private applyTheme(mode: ThemeMode) {
    document.documentElement.setAttribute('data-theme', mode);
  }
}
