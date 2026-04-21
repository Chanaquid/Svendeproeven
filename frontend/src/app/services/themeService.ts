import { Injectable, effect, signal } from '@angular/core';

export type Theme = 'light' | 'dark';

/**
 * ThemeService — manages light/dark mode across the app.
 *
 * Default is 'light' on first visit. The user's choice is persisted to
 * localStorage and restored on subsequent visits. The active theme is applied
 * by setting `data-theme` on <html>, which flips the CSS custom properties
 * defined in styles.css.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly STORAGE_KEY = 'rentit-theme';

  readonly theme = signal<Theme>(this.loadInitial());

  constructor() {
    // Keep <html data-theme="..."> and localStorage in sync with the signal.
    effect(() => {
      const t = this.theme();
      document.documentElement.setAttribute('data-theme', t);
      try {
        localStorage.setItem(this.STORAGE_KEY, t);
      } catch {
        // localStorage can throw in private browsing mode — safe to ignore.
      }
    });
  }

  toggle(): void {
    this.theme.update((t) => (t === 'light' ? 'dark' : 'light'));
  }

  set(theme: Theme): void {
    this.theme.set(theme);
  }

  private loadInitial(): Theme {
    try {
      const saved = localStorage.getItem(this.STORAGE_KEY);
      if (saved === 'light' || saved === 'dark') return saved;
    } catch {
      // ignore
    }
    return 'light';
  }
}
