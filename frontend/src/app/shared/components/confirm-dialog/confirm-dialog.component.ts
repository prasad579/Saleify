import { Component, inject } from '@angular/core';
import { ConfirmDialogService } from '@core/services/confirm-dialog.service';

/** Global confirm dialog. Mounted once in the shell; driven by ConfirmDialogService. */
@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  templateUrl: './confirm-dialog.component.html',
  styleUrl: './confirm-dialog.component.scss'
})
export class ConfirmDialogComponent {
  svc = inject(ConfirmDialogService);
}
