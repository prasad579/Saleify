import { Component, OnInit } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '@core/services/auth.service';
import { ApiHealthService } from '@core/services/api-health.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss'
})
export class ShellComponent implements OnInit {
  constructor(
    public auth: AuthService,
    private router: Router,
    public health: ApiHealthService
  ) {}

  ngOnInit() {
    this.health.check();
  }

  signOut() {
    this.auth.signOut(this.router);
  }

  navItems = [
    { path: '/home', icon: '🏠', label: 'Home' },
    { path: '/deals', icon: '💼', label: 'Deals', badge: 0 },
    { path: '/deals/new', icon: '➕', label: 'New Deal' },
  ];
}
