import { Injectable, signal } from '@angular/core';

export type ToastType = 'success' | 'error' | 'info';

export interface Toast {
  id: number;
  message: string;
  type: ToastType;
}

/**
 * Drives the global toast stack (mounted once in the shell). Call success()/error()/info()
 * to show a transient notification; each auto-dismisses after a few seconds.
 */
@Injectable({ providedIn: 'root' })
export class ToastService {
  readonly toasts = signal<Toast[]>([]);
  private nextId = 1;

  success(message: string, durationMs = 3500) { this.show(message, 'success', durationMs); }
  error(message: string, durationMs = 5000) { this.show(message, 'error', durationMs); }
  info(message: string, durationMs = 3500) { this.show(message, 'info', durationMs); }

  show(message: string, type: ToastType = 'info', durationMs = 3500) {
    if (!message) return;
    const id = this.nextId++;
    this.toasts.update(list => [...list, { id, message, type }]);
    if (durationMs > 0) {
      setTimeout(() => this.dismiss(id), durationMs);
    }
  }

  dismiss(id: number) {
    this.toasts.update(list => list.filter(t => t.id !== id));
  }
}
