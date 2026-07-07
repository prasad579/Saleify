import { Component, signal } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { AuthService } from '@core/services/auth.service';
import { ToastComponent } from '@shared/components/toast/toast.component';

@Component({
  selector: 'app-customer-shell',
  standalone: true,
  imports: [RouterOutlet, ToastComponent],
  templateUrl: './customer-shell.component.html',
  styleUrl: './customer-shell.component.scss'
})
export class CustomerShellComponent {
  menuOpen = signal(false);

  constructor(public auth: AuthService, private router: Router) {}

  toggleMenu() {
    this.menuOpen.update(v => !v);
  }

  signOut() {
    this.auth.signOut(this.router);
  }

  initials() {
    const name = this.auth.user()?.name || 'JR';
    return name.split(' ').map(p => p[0]).join('').slice(0, 2).toUpperCase();
  }
}
