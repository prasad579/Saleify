import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ApiService } from '@core/services/api.service';
import { ApiHealthService } from '@core/services/api-health.service';
import { apiErrorMessage, extractCreatedDealId, isCreateDealSuccess, normalizeDealDetail } from '@shared/utils/deal-api.util';
import { COUNTRIES, SAAS_INDUSTRIES, DEAL_TYPES, MARKETPLACES } from '@shared/data/lookups';
import { DealFlowFooterComponent } from '@shared/components/deal-flow-footer/deal-flow-footer.component';

@Component({
  selector: 'app-deal-create',
  standalone: true,
  imports: [FormsModule, RouterLink, DealFlowFooterComponent],
  templateUrl: './deal-create.component.html',
  styleUrl: './deal-create.component.scss'
})
export class DealCreateComponent implements OnInit {
  private api = inject(ApiService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  health = inject(ApiHealthService);

  countries = COUNTRIES;
  industries = SAAS_INDUSTRIES;
  dealTypes = DEAL_TYPES;
  marketplaces = MARKETPLACES;

  editId = '';
  isEdit = false;
  saving = false;
  loading = false;
  error = '';
  success = '';

  deal = {
    name: '',
    customer: '',
    contactName: '',
    contactEmail: '',
    location: '',
    industry: '',
    marketplace: '',
    dealType: 'New Deal',
    expectedValue: null as number | null,
    expectedCloseDate: '',
    description: ''
  };

  ngOnInit() {
    this.editId = this.route.snapshot.paramMap.get('id') || '';
    this.isEdit = !!this.editId && this.editId !== 'new';
    this.health.check();
    this.api.getLookups().subscribe({
      next: (data: any) => {
        if (data.countries?.length) this.countries = data.countries;
        if (data.industries?.length) this.industries = data.industries;
        if (data.dealTypes?.length) this.dealTypes = data.dealTypes;
        if (data.marketplaces?.length) this.marketplaces = data.marketplaces;
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
          this.error = 'Deal not found.';
          this.loading = false;
          return;
        }
        this.deal = {
          name: d.name || '',
          customer: d.customer || '',
          contactName: d.contactName || '',
          contactEmail: d.contactEmail || '',
          location: d.location || '',
          industry: d.industry || '',
          marketplace: d.marketplace || '',
          dealType: d.dealType || 'New Deal',
          expectedValue: d.expectedValue ?? null,
          expectedCloseDate: d.expectedCloseDate?.slice(0, 10) || '',
          description: d.description || ''
        };
        this.loading = false;
      },
      error: (err) => {
        this.error = apiErrorMessage(err, 'Could not load deal.');
        this.loading = false;
      }
    });
  }

  validate(): string | null {
    if (!this.deal.customer.trim()) return 'Customer / Company is required.';
    if (!this.deal.contactName.trim()) return 'Primary contact name is required.';
    if (!this.deal.contactEmail.trim()) return 'Contact email is required.';
    if (!this.deal.marketplace) return 'Marketplace is required.';
    if (!this.deal.expectedCloseDate) return 'Expected close date is required.';
    return null;
  }

  submit() {
    this.error = '';
    this.success = '';
    const validationError = this.validate();
    if (validationError) {
      this.error = validationError;
      return;
    }

    this.saving = true;
    const payload = {
      ...this.deal,
      expectedValue: this.deal.expectedValue ?? 0
    };

    if (this.isEdit) {
      this.api.updateDeal(this.editId, { ...payload, id: this.editId }).subscribe({
        next: () => {
          this.saving = false;
          this.success = 'Deal updated.';
          setTimeout(() => this.router.navigate(['/deals', this.editId, 'products']), 400);
        },
        error: (err) => {
          this.saving = false;
          this.error = apiErrorMessage(err, 'Could not update deal.');
        }
      });
      return;
    }

    this.api.createDeal(payload).subscribe({
      next: (res: any) => {
        this.saving = false;
        const id = extractCreatedDealId(res);
        if (!isCreateDealSuccess(res, id)) {
          this.error = res?.message || 'Deal was not created. Please try again.';
          return;
        }
        this.success = res?.message || `Deal ${id} created!`;
        setTimeout(() => this.router.navigate(['/deals', id, 'products']), 600);
      },
      error: (err) => {
        this.saving = false;
        this.error = apiErrorMessage(err, 'Could not save deal.');
      }
    });
  }
}
