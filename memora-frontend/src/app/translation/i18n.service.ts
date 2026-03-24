import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export type AppLanguage = 'de' | 'en';

type TranslationParams = Record<string, string | number | null | undefined>;

@Injectable({ providedIn: 'root' })
export class I18nService {
  private readonly storageKey = 'memora.language';

  private translations: any = {};
  private currentLang: AppLanguage = this.getInitialLanguage();

  private readonly languageSubject = new BehaviorSubject<AppLanguage>(this.currentLang);
  readonly language$ = this.languageSubject.asObservable();

  async init() {
    await this.loadTranslations(this.currentLang);
  }

  get currentLanguage(): AppLanguage {
    return this.currentLang;
  }

  async setLanguage(language: AppLanguage) {
    if (language == this.currentLang) return;

    this.currentLang = language;
    await this.loadTranslations(language);

    localStorage.setItem(this.storageKey, language);
    document.documentElement.lang = language;

    this.languageSubject.next(language);
  }

  private async loadTranslations(lang: AppLanguage) {
    try {
      const res = await fetch(`/i18n_data/${lang}.json`);
      this.translations = await res.json();
    } catch {
      this.translations = {};
    }
  }

  translate(key: string, params?: TranslationParams): string {
    const template = this.getNestedValue(this.translations, key) ?? key;
    return this.interpolate(template, params);
  }

  private getNestedValue(obj: any, path: string): string | undefined {
    return path.split('.').reduce((acc, part) => acc?.[part], obj);
  }

  private interpolate(template: string, params?: TranslationParams): string {
    if (!params) return template;

    return template.replace(/\{(\w+)\}/g, (_, param: string) => {
      const value = params[param];
      return value != null ? String(value) : `{${param}}`;
    });
  }

  private getInitialLanguage(): AppLanguage {
    const stored = localStorage.getItem(this.storageKey);

    if (stored === 'de' || stored === 'en') {
      document.documentElement.lang = stored;
      return stored;
    }

    const browser = navigator.language.toLowerCase().startsWith('de') ? 'de' : 'en';
    document.documentElement.lang = browser;
    return browser;
  }
}
