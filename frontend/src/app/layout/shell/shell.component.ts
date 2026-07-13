import { Component, OnInit } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '@core/services/auth.service';
import { ApiHealthService } from '@core/services/api-health.service';
import { EngagementSearchComponent } from '@shared/components/engagement-search/engagement-search.component';
import { EngagementSnapshotComponent } from '@shared/components/engagement-snapshot/engagement-snapshot.component';
import { ConfirmDialogComponent } from '@shared/components/confirm-dialog/confirm-dialog.component';
import { PageNavComponent } from '@shared/components/page-nav/page-nav.component';
import { ToastComponent } from '@shared/components/toast/toast.component';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, EngagementSearchComponent, EngagementSnapshotComponent, ConfirmDialogComponent, PageNavComponent, ToastComponent],
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
    { path: '/deals', icon: '💼', label: 'Engagements', badge: 0 },
    { path: '/deals/new', icon: '➕', label: 'New Engagement' },
    { path: '/offer-requests', icon: '📨', label: 'Offer Requests' },
    { path: '/engagement-requests', icon: '📥', label: 'Customer Requests' },
    { path: '/audit-log', icon: '📜', label: 'Audit Log' },
    { path: '/settings', icon: '⚙️', label: 'Settings' },
  ];
}
