import { Component, inject } from '@angular/core';
import { ToastService } from '@core/services/toast.service';

/** Global toast stack. Mounted once in the shell; driven by ToastService. */
@Component({
  selector: 'app-toast',
  standalone: true,
  templateUrl: './toast.component.html',
  styleUrl: './toast.component.scss'
})
export class ToastComponent {
  svc = inject(ToastService);

  icon(type: string): string {
    switch (type) {
      case 'success': return '✓';
      case 'error': return '⚠';
      default: return 'ℹ';
    }
  }
}
