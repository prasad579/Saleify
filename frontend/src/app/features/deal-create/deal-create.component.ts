import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ApiService } from '@core/services/api.service';
import { ApiHealthService } from '@core/services/api-health.service';
import { EngagementConfigService } from '@core/services/engagement-config.service';
import { ToastService } from '@core/services/toast.service';
import { apiErrorMessage, extractCreatedDealId, isCreateDealSuccess, normalizeDealDetail } from '@shared/utils/deal-api.util';
import {
  COUNTRIES, SAAS_INDUSTRIES, DEAL_TYPES, MARKETPLACES,
  DEAL_OWNERS, PRIORITIES, CampaignEvent, Person, eventStatus,
  MARKETPLACE_ACCOUNTS, MarketplaceAccountField, NO_EVENT_TAG
} from '@shared/data/lookups';
import { EngagementTypeConfig, ScreenKey, enabledEngagementConfigs, getEngagementConfig, nextScreenPath, stepperSteps } from '@shared/utils/engagement.util';
import { DealFlowFooterComponent } from '@shared/components/deal-flow-footer/deal-flow-footer.component';
import { SettingsHintComponent } from '@shared/components/settings-hint/settings-hint.component';

interface CustomerSummary {
  name: string;
  totalDeals: number;
  openDeals: number;
  privateOffers: number;
  lastActivity: string;
}

@Component({
  selector: 'app-deal-create',
  standalone: true,
  imports: [FormsModule, RouterLink, DealFlowFooterComponent, SettingsHintComponent],
  templateUrl: './deal-create.component.html',
  styleUrl: './deal-create.component.scss'
})
export class DealCreateComponent implements OnInit {
  private api = inject(ApiService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private engagementCfg = inject(EngagementConfigService);
  private toast = inject(ToastService);
  health = inject(ApiHealthService);

  countries = COUNTRIES;
  industries = SAAS_INDUSTRIES;
  dealTypes = DEAL_TYPES;
  marketplaces = MARKETPLACES;
  owners = DEAL_OWNERS;
  priorities = PRIORITIES;
  engagementConfigs = enabledEngagementConfigs();

  events: CampaignEvent[] = [];
  playbooks: any[] = [];
  people: Person[] = [];
  noEventTag = NO_EVENT_TAG;

  /**
   * Owners offered in the dropdown: enabled people from Settings → People who are eligible
   * for the chosen engagement type (a person with no engagement-type restriction is eligible
   * for all). Falls back to the static lookup list when People settings are empty/unavailable.
   */
  get eligibleOwners(): { name: string; role: string }[] {
    const type = this.deal.engagementType;
    const eligible = this.people
      .filter(p => p.enabled)
      .filter(p => !p.engagementTypes?.length || (!!type && p.engagementTypes.includes(type)));
    if (!eligible.length) return this.owners.map(name => ({ name, role: '' }));
    return eligible.map(p => ({ name: p.name, role: p.role }));
  }

  /** Marketplace is derived from (and locked to) the tagged campaign / event when it carries one. */
  get marketplaceLocked(): boolean {
    return !!this.selectedEvent?.marketplace;
  }

  /** Tags shown in the dropdown: active (non-paused) events, plus the current one when editing. */
  get selectableEvents(): CampaignEvent[] {
    return this.events.filter(e => !e.paused || e.id === this.deal.campaignEventId);
  }

  editId = '';
  isEdit = false;
  saving = false;
  loading = false;
  error = '';
  success = '';
  /** True when editing an engagement that was submitted as an offer request (details locked). */
  locked = false;
  unlocking = false;

  /** 'quick' = minimal fields, save & finish later; 'full' = all fields + continue workflow. */
  mode: 'quick' | 'full' = 'full';

  selectedMarketplaces: string[] = [];
  existingCustomer: CustomerSummary | null = null;
  private allDeals: any[] = [];

  /** marketplace -> billing account id. */
  billingAccountIds: Record<string, string> = {};

  // Track which fields were auto-filled from the work email so we don't clobber manual edits.
  private companyAutoFilled = true;
  private contactAutoFilled = true;

  deal = {
    name: '',
    contactEmail: '',
    customer: '',
    contactName: '',
    phone: '',
    priority: '',
    location: '',
    industry: '',
    marketplace: '',
    dealType: 'New Deal',
    engagementType: '',
    campaignEventId: '',
    campaignEventName: '',
    owner: '',
    expectedValue: null as number | null,
    expectedCloseDate: '',
    description: ''
  };

  get config(): EngagementTypeConfig {
    return getEngagementConfig(this.deal.engagementType);
  }

  /** Step 1 is "done" once an engagement type is chosen. */
  get typeChosen(): boolean {
    return !!this.deal.engagementType;
  }

  get tagRequired(): boolean {
    return this.config.tagRequired;
  }

  get marketplaceRequired(): boolean {
    return this.config.marketplaceRequired;
  }

  get flowSteps(): { key: ScreenKey; label: string }[] {
    return stepperSteps(this.deal.engagementType || 'Private Offer');
  }

  get isQuick(): boolean { return this.mode === 'quick'; }

  setMode(mode: 'quick' | 'full'): void { this.mode = mode; }

  /** Configurable "what's next" guidance for the selected engagement type. */
  get playbook(): any | null {
    return this.playbooks.find(p => p.engagementType === this.deal.engagementType) ?? null;
  }

  /** Collapsed state for the playbook card — remembered across visits to save space. */
  playbookCollapsed = false;

  togglePlaybook(): void {
    this.playbookCollapsed = !this.playbookCollapsed;
    try { localStorage.setItem('playbookCollapsed', this.playbookCollapsed ? '1' : '0'); } catch { /* ignore */ }
  }

  get accountFields(): MarketplaceAccountField[] {
    return MARKETPLACE_ACCOUNTS.filter(a => this.selectedMarketplaces.includes(a.marketplace));
  }

  get selectedEvent(): CampaignEvent | null {
    return this.events.find(e => e.id === this.deal.campaignEventId) ?? null;
  }

  get allMarketplacesSelected(): boolean {
    return this.marketplaces.length > 0 &&
      this.marketplaces.every(m => this.selectedMarketplaces.includes(m));
  }

  isMarketplaceSelected(m: string): boolean {
    return this.selectedMarketplaces.includes(m);
  }

  toggleMarketplace(m: string): void {
    this.selectedMarketplaces = this.selectedMarketplaces.includes(m)
      ? this.selectedMarketplaces.filter(x => x !== m)
      : [...this.selectedMarketplaces, m];
    this.clearError('marketplace');
  }

  toggleAllMarketplaces(): void {
    this.selectedMarketplaces = this.allMarketplacesSelected ? [] : [...this.marketplaces];
    this.clearError('marketplace');
  }

  selectEngagementType(type: string): void {
    this.deal.engagementType = type;
    this.clearError('engagementType');
    // Drop the owner if they aren't eligible to own this engagement type.
    if (this.deal.owner && !this.eligibleOwners.some(o => o.name === this.deal.owner)) {
      this.deal.owner = '';
    }
    // Clear tag if it no longer applies and isn't required.
    this.maybeSuggestName();
  }

  onEventChange(): void {
    this.deal.campaignEventName = this.resolveEventName();
    this.applyEventMarketplace();
  }

  /** When a campaign / event with a marketplace is tagged, force the deal to that marketplace. */
  private applyEventMarketplace(): void {
    const ev = this.selectedEvent;
    if (ev?.marketplace) {
      this.selectedMarketplaces = [ev.marketplace];
    }
  }

  private resolveEventName(): string {
    if (this.deal.campaignEventId === this.noEventTag.id) return this.noEventTag.name;
    return this.selectedEvent?.name ?? '';
  }

  statusBadgeClass(status: string): string {
    switch (status) {
      case 'Active': return 'badge-green';
      case 'Upcoming': return 'badge-blue';
      case 'Completed': return 'badge-orange';
      default: return 'badge-gray';
    }
  }

  eventStatusOf(ev: CampaignEvent): string {
    return ev.status || eventStatus(ev.startDate, ev.endDate);
  }

  private titleCase(value: string): string {
    return value
      .split(/[.\-_\s]+/)
      .filter(Boolean)
      .map(p => p.charAt(0).toUpperCase() + p.slice(1))
      .join(' ');
  }

  /** Derive company + contact from the work email, then look up existing-customer stats. */
  onWorkEmailChange(): void {
    const email = this.deal.contactEmail.trim();
    const at = email.indexOf('@');
    if (at > 0 && email.indexOf('.', at) > at) {
      const local = email.slice(0, at);
      const domain = email.slice(at + 1);
      const companyToken = domain.split('.')[0];
      if (this.companyAutoFilled && !this.deal.customer.trim()) {
        this.deal.customer = this.titleCase(companyToken);
      } else if (this.companyAutoFilled) {
        this.deal.customer = this.titleCase(companyToken);
      }
      if (this.contactAutoFilled) {
        this.deal.contactName = this.titleCase(local);
      }
    }
    this.lookupExistingCustomer();
    this.maybeSuggestName();
  }

  onCustomerEdited(): void { this.companyAutoFilled = false; this.lookupExistingCustomer(); this.maybeSuggestName(); }
  onContactEdited(): void { this.contactAutoFilled = false; }

  private maybeSuggestName(): void {
    // Auto-name unless the user typed their own name.
    if (this.deal.name && this.deal.name.includes(' — ') === false && this.deal.name.trim()) {
      // user-provided custom name; leave it
    }
    if (!this.userNamed) {
      const parts = [this.deal.engagementType, this.deal.customer].filter(Boolean);
      this.deal.name = parts.join(' — ');
    }
  }

  private userNamed = false;
  onNameEdited(): void { this.userNamed = true; }

  /** Match existing deals by work-email domain or company name to build the customer card. */
  private lookupExistingCustomer(): void {
    const email = this.deal.contactEmail.trim().toLowerCase();
    const domain = email.includes('@') ? email.split('@')[1] : '';
    const company = this.deal.customer.trim().toLowerCase();

    const matches = this.allDeals.filter(d => {
      if (d.id === this.editId) return false;
      const dEmail = (d.contactEmail || '').toLowerCase();
      const dDomain = dEmail.includes('@') ? dEmail.split('@')[1] : '';
      const dCompany = (d.customer || '').trim().toLowerCase();
      return (domain && dDomain === domain) || (company && dCompany === company);
    });

    if (!matches.length) { this.existingCustomer = null; return; }

    const openStatuses = ['Draft', 'In Review', 'Waiting for Info', 'Lead'];
    const lastActivity = matches
      .map(d => d.lastUpdated || d.createdAt || '')
      .filter(Boolean)
      .sort()
      .pop() || '—';

    this.existingCustomer = {
      name: matches[0].customer,
      totalDeals: matches.length,
      openDeals: matches.filter(d => openStatuses.includes(d.marketplaceStatus)).length,
      privateOffers: matches.filter(d => d.engagementType === 'Private Offer').length,
      lastActivity
    };
  }

  viewCustomerHistory(): void {
    if (!this.existingCustomer) return;
    this.router.navigate(['/deals'], { queryParams: { q: this.existingCustomer.name } });
  }

  ngOnInit() {
    this.editId = this.route.snapshot.paramMap.get('id') || '';
    this.isEdit = !!this.editId && this.editId !== 'new';
    this.health.check();
    try { this.playbookCollapsed = localStorage.getItem('playbookCollapsed') === '1'; } catch { /* ignore */ }

    // Refresh the engagement catalog from settings so disabling/enabling a type in
    // Settings → Engagement Types is reflected here without a full page reload.
    this.engagementCfg.load().then(() => {
      this.engagementConfigs = enabledEngagementConfigs();
      // If the previously-selected type was disabled, drop it.
      if (this.deal.engagementType && !this.engagementConfigs.some(c => c.type === this.deal.engagementType)) {
        this.deal.engagementType = '';
      }
    });

    this.api.getLookups().subscribe({
      next: (data: any) => {
        if (data.countries?.length) this.countries = data.countries;
        if (data.industries?.length) this.industries = data.industries;
        if (data.dealTypes?.length) this.dealTypes = data.dealTypes;
        if (data.marketplaces?.length) this.marketplaces = data.marketplaces;
        if (data.dealOwners?.length) this.owners = data.dealOwners;
        if (data.priorities?.length) this.priorities = data.priorities;
      }
    });

    this.api.getCampaignEvents().subscribe({
      next: (data: any) => {
        this.events = Array.isArray(data) ? data : [];
        // Events may load after the deal when editing — re-derive the locked marketplace.
        this.applyEventMarketplace();
      }
    });

    this.api.getPeople().subscribe({
      next: (data) => { this.people = Array.isArray(data) ? data : []; }
    });

    this.api.getPlaybooks().subscribe({
      next: (data: any) => { this.playbooks = Array.isArray(data) ? data : []; }
    });

    this.api.getDeals().subscribe({
      next: (data: any) => {
        this.allDeals = Array.isArray(data) ? data : [];
        this.lookupExistingCustomer();
      }
    });

    if (this.isEdit) this.loadDeal();
  }

  loadDeal() {
    this.loading = true;
    this.api.getDeal(this.editId).subscribe({
      next: (raw: any) => {
        const d = normalizeDealDetail(raw).deal;
        if (!d) {
          this.error = 'Engagement not found.';
          this.loading = false;
          return;
        }
        this.deal = {
          name: d.name || '',
          contactEmail: d.contactEmail || '',
          customer: d.customer || '',
          contactName: d.contactName || '',
          phone: d.phone || '',
          priority: d.priority || '',
          location: d.location || '',
          industry: d.industry || '',
          marketplace: d.marketplace || '',
          dealType: d.dealType || 'New Deal',
          engagementType: d.engagementType || 'Private Offer',
          campaignEventId: d.campaignEventId || '',
          campaignEventName: d.campaignEventName || '',
          owner: d.owner || '',
          expectedValue: d.expectedValue ?? null,
          expectedCloseDate: d.expectedCloseDate?.slice(0, 10) || '',
          description: d.description || ''
        };
        this.locked = !!d.locked;
        this.userNamed = !!this.deal.name;
        this.mode = d.quickCapture ? 'quick' : 'full';
        this.companyAutoFilled = false;
        this.contactAutoFilled = false;
        this.billingAccountIds = { ...(d.billingAccountIds || {}) };
        this.selectedMarketplaces = (d.marketplace || '')
          .split(',')
          .map((s: string) => s.trim())
          .filter((s: string) => s.length > 0);
        this.lookupExistingCustomer();
        this.loading = false;
      },
      error: (err) => {
        this.error = apiErrorMessage(err, 'Could not load engagement.');
        this.loading = false;
      }
    });
  }

  private emailValid(email: string): boolean {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim());
  }

  /** Per-field validation messages, shown inline next to each field. */
  fieldErrors: Record<string, string> = {};

  /** Validate every field, populating fieldErrors. Returns true when the form is valid. */
  validateFields(): boolean {
    const e: Record<string, string> = {};
    if (!this.deal.engagementType) e['engagementType'] = 'Select what you want to create first.';
    if (!this.deal.contactEmail.trim()) e['contactEmail'] = 'Work email is required.';
    else if (!this.emailValid(this.deal.contactEmail)) e['contactEmail'] = 'Enter a valid email address (name@company.com).';
    if (!this.deal.owner) e['owner'] = 'Select an engagement owner.';
    if (this.marketplaceRequired && this.selectedMarketplaces.length === 0) e['marketplace'] = 'Select at least one marketplace.';
    if (this.tagRequired && !this.deal.campaignEventId) e['campaignEventId'] = 'A campaign / event tag is required for this engagement type.';
    this.fieldErrors = e;
    return Object.keys(e).length === 0;
  }

  /** Clear the inline error for a field once the user starts fixing it. */
  clearError(field: string) {
    if (this.fieldErrors[field]) {
      const { [field]: _removed, ...rest } = this.fieldErrors;
      this.fieldErrors = rest;
    }
  }

  /** Unlock a submitted engagement so its details can be revised. */
  unlock() {
    if (!this.editId) return;
    this.unlocking = true;
    this.api.unlockEngagementEdits(this.editId).subscribe({
      next: (r: any) => {
        this.unlocking = false;
        this.locked = false;
        this.toast.success(r?.message || 'Engagement unlocked. Edit and re-submit to push the changes.');
      },
      error: (e) => { this.unlocking = false; this.toast.error(apiErrorMessage(e, 'Could not unlock the engagement.')); }
    });
  }

  submit() {
    this.error = '';
    this.success = '';
    if (this.locked) {
      this.toast.error('This engagement is locked. Unlock it to revise.');
      return;
    }
    if (!this.validateFields()) {
      this.toast.error('Please fix the highlighted fields.');
      return;
    }

    this.deal.marketplace = this.marketplaces
      .filter(m => this.selectedMarketplaces.includes(m))
      .join(', ');
    this.deal.campaignEventName = this.resolveEventName();
    if (!this.deal.name.trim()) {
      this.deal.name = [this.deal.engagementType, this.deal.customer].filter(Boolean).join(' — ') || this.deal.engagementType;
    }

    const billingAccountIds: Record<string, string> = {};
    for (const m of this.selectedMarketplaces) {
      const val = (this.billingAccountIds[m] || '').trim();
      if (val) billingAccountIds[m] = val;
    }

    this.saving = true;
    const payload = { ...this.deal, billingAccountIds, quickCapture: this.isQuick, expectedValue: this.deal.expectedValue ?? 0 };

    if (this.isEdit) {
      this.api.updateDeal(this.editId, { ...payload, id: this.editId }).subscribe({
        next: () => {
          this.saving = false;
          this.toast.success(this.isQuick ? 'Quick capture saved.' : 'Engagement updated.');
          this.afterSave(this.editId);
        },
        error: (err) => {
          this.saving = false;
          this.toast.error(apiErrorMessage(err, 'Could not update engagement.'));
        }
      });
      return;
    }

    this.api.createDeal(payload).subscribe({
      next: (res: any) => {
        this.saving = false;
        const id = extractCreatedDealId(res);
        if (!isCreateDealSuccess(res, id)) {
          this.toast.error(res?.message || 'Engagement was not created. Please try again.');
          return;
        }
        this.toast.success(res?.message || `Engagement ${id} ${this.isQuick ? 'captured' : 'created'}!`);
        this.afterSave(id!);
      },
      error: (err) => {
        this.saving = false;
        this.toast.error(apiErrorMessage(err, 'Could not save engagement.'));
      }
    });
  }

  /** Quick capture returns to the list to finish later; full continues into the workflow. */
  private afterSave(id: string) {
    if (this.isQuick) {
      setTimeout(() => this.router.navigateByUrl('/deals'), 500);
      return;
    }
    this.goToNext(id);
  }

  /** Navigate to the next applicable screen for this engagement type (skips hidden screens). */
  private goToNext(id: string) {
    const next = nextScreenPath(this.deal.engagementType, id, 'details');
    setTimeout(() => this.router.navigateByUrl(next ?? `/deals/${id}`), 500);
  }
}
