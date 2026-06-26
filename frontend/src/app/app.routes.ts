import { Routes } from '@angular/router';
import { ShellComponent } from '@layout/shell/shell.component';
import { LoginComponent } from '@features/login/login.component';
import { SignupComponent } from '@features/signup/signup.component';
import { AuthCallbackComponent } from '@features/auth/auth-callback.component';
import { ForgotPasswordComponent } from '@features/auth/forgot-password.component';
import { ResetSentComponent } from '@features/auth/reset-sent.component';
import { VerifyEmailComponent } from '@features/auth/verify-email.component';
import { EmailVerifiedComponent } from '@features/auth/email-verified.component';
import { AwaitingRoleComponent } from '@features/auth/awaiting-role.component';
import { AccessGrantedComponent } from '@features/auth/access-granted.component';
import { HomeComponent } from '@features/home/home.component';
import { DealsListComponent } from '@features/deals-list/deals-list.component';
import { DealCreateComponent } from '@features/deal-create/deal-create.component';
import { DealProductsComponent } from '@features/deal-products/deal-products.component';
import { DealPricingComponent } from '@features/deal-pricing/deal-pricing.component';
import { DealOverviewComponent } from '@features/deal-overview/deal-overview.component';
import { DealMeetingNotesComponent } from '@features/deal-meeting-notes/deal-meeting-notes.component';
import { DealApprovalsComponent } from '@features/deal-approvals/deal-approvals.component';
import { CampaignEventsComponent } from '@features/campaign-events/campaign-events.component';
import { PlaybookSettingsComponent } from '@features/playbook-settings/playbook-settings.component';
import { SnapshotSettingsComponent } from '@features/snapshot-settings/snapshot-settings.component';
import { ApprovalSettingsComponent } from '@features/approval-settings/approval-settings.component';
import { PeopleSettingsComponent } from '@features/people-settings/people-settings.component';
import { EngagementTypesComponent } from '@features/engagement-types/engagement-types.component';
import { HomeSettingsComponent } from '@features/home-settings/home-settings.component';
import { AuditLogComponent } from '@features/audit-log/audit-log.component';
import { OfferRequestsComponent } from '@features/offer-requests/offer-requests.component';
import { OfferRequestDetailComponent } from '@features/offer-request-detail/offer-request-detail.component';
import { SettingsHomeComponent } from '@features/settings/settings-home.component';
import { authGuard, guestGuard } from '@core/guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: 'login', component: LoginComponent, canActivate: [guestGuard] },
  { path: 'auth/callback', component: AuthCallbackComponent },
  { path: 'signup', component: SignupComponent, canActivate: [guestGuard] },
  { path: 'forgot-password', component: ForgotPasswordComponent, canActivate: [guestGuard] },
  { path: 'reset-sent', component: ResetSentComponent, canActivate: [guestGuard] },
  { path: 'signup/verify-email', component: VerifyEmailComponent },
  { path: 'signup/email-verified', component: EmailVerifiedComponent },
  { path: 'signup/awaiting-role', component: AwaitingRoleComponent },
  { path: 'signup/access-granted', component: AccessGrantedComponent },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      { path: 'home', component: HomeComponent },
      { path: 'deals', component: DealsListComponent },
      { path: 'settings', component: SettingsHomeComponent },
      { path: 'settings/campaign-events', component: CampaignEventsComponent },
      { path: 'settings/playbooks', component: PlaybookSettingsComponent },
      { path: 'settings/snapshot', component: SnapshotSettingsComponent },
      { path: 'settings/approvals', component: ApprovalSettingsComponent },
      { path: 'settings/engagement-types', component: EngagementTypesComponent },
      { path: 'settings/home', component: HomeSettingsComponent },
      { path: 'settings/people', component: PeopleSettingsComponent },
      { path: 'offer-requests', component: OfferRequestsComponent },
      { path: 'offer-requests/:id', component: OfferRequestDetailComponent },
      { path: 'audit-log', component: AuditLogComponent },
      { path: 'campaign-events', redirectTo: 'settings/campaign-events', pathMatch: 'full' },
      { path: 'deals/new', component: DealCreateComponent },
      { path: 'deals/:id/edit', component: DealCreateComponent },
      { path: 'deals/:id/products', component: DealProductsComponent },
      { path: 'deals/:id/pricing', component: DealPricingComponent },
      { path: 'deals/:id/meeting-notes', component: DealMeetingNotesComponent },
      { path: 'deals/:id/approvals', component: DealApprovalsComponent },
      { path: 'deals/:id', component: DealOverviewComponent },
    ]
  },
  { path: '**', redirectTo: 'login' }
];
