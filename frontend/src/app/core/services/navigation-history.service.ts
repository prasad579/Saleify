import { Injectable, signal } from '@angular/core';
import { NavigationEnd, NavigationStart, Router } from '@angular/router';

/**
 * Tracks an in-app navigation stack so the page-level Back / Forward buttons
 * know where they can go and stay in sync with the browser back/forward too.
 */
@Injectable({ providedIn: 'root' })
export class NavigationHistoryService {
  private stack: string[] = [];
  private index = -1;
  private internalNav = false;
  private lastTrigger: 'imperative' | 'popstate' | 'hashchange' | null = null;

  readonly canBack = signal(false);
  readonly canForward = signal(false);

  constructor(private router: Router) {
    this.router.events.subscribe(e => {
      if (e instanceof NavigationStart) {
        this.lastTrigger = e.navigationTrigger ?? 'imperative';
      } else if (e instanceof NavigationEnd) {
        this.onNavigated(e.urlAfterRedirects);
      }
    });
  }

  private onNavigated(url: string) {
    if (this.internalNav) {
      // Driven by our own back()/forward() — index already moved.
      this.internalNav = false;
    } else if (this.lastTrigger === 'popstate') {
      // Browser back/forward — sync the pointer to the matching neighbour, else treat as new.
      if (this.index > 0 && this.stack[this.index - 1] === url) this.index--;
      else if (this.index < this.stack.length - 1 && this.stack[this.index + 1] === url) this.index++;
      else this.pushNew(url);
    } else if (this.stack[this.index] !== url) {
      this.pushNew(url);
    }
    this.refresh();
  }

  private pushNew(url: string) {
    this.stack = this.stack.slice(0, this.index + 1);
    this.stack.push(url);
    this.index = this.stack.length - 1;
  }

  private refresh() {
    this.canBack.set(this.index > 0);
    this.canForward.set(this.index < this.stack.length - 1);
  }

  back() {
    if (this.index <= 0) return;
    this.index--;
    this.internalNav = true;
    this.router.navigateByUrl(this.stack[this.index]);
    this.refresh();
  }

  forward() {
    if (this.index >= this.stack.length - 1) return;
    this.index++;
    this.internalNav = true;
    this.router.navigateByUrl(this.stack[this.index]);
    this.refresh();
  }
}
