import { Injectable, signal } from '@angular/core';

export interface ConfirmConfig {
  title: string;
  /** Body lines rendered as paragraphs. */
  lines: string[];
  confirmLabel: string;
  cancelLabel?: string;
  /** Red, destructive styling for the confirm button. */
  danger?: boolean;
}

/**
 * Drives a single global confirm dialog (mounted once in the shell).
 * `open()` returns a promise that resolves true (confirmed) or false (cancelled).
 */
@Injectable({ providedIn: 'root' })
export class ConfirmDialogService {
  readonly config = signal<ConfirmConfig | null>(null);
  private resolver: ((value: boolean) => void) | null = null;

  open(config: ConfirmConfig): Promise<boolean> {
    this.config.set(config);
    return new Promise<boolean>(resolve => { this.resolver = resolve; });
  }

  resolve(value: boolean) {
    this.config.set(null);
    const r = this.resolver;
    this.resolver = null;
    r?.(value);
  }
}
