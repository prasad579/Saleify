import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '@core/services/api.service';
import { AuthService } from '@core/services/auth.service';
import { ToastService } from '@core/services/toast.service';
import {
  MARKETPLACES,
  ENGAGEMENT_REQUEST_TYPES,
  REQUEST_DURATIONS,
  ESTIMATED_USER_RANGES,
  BUDGET_RANGES,
  CONTACT_PREFERENCES,
  CONTACT_TIME_SLOTS
} from '@shared/data/lookups';

type WizardStep = 'details' | 'review';

@Component({
  selector: 'app-engagement-request-new',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './engagement-request-new.component.html',
  styleUrl: './engagement-request-new.component.scss'
})
export class EngagementRequestNewComponent implements OnInit {
  private api = inject(ApiService);
  private auth = inject(AuthService);
  private router = inject(Router);
  private toast = inject(ToastService);

  requestTypes = ENGAGEMENT_REQUEST_TYPES;
  marketplaces = MARKETPLACES;
  durations = REQUEST_DURATIONS;
  userRanges = ESTIMATED_USER_RANGES;
  budgetRanges = BUDGET_RANGES;
  contactPreferences = CONTACT_PREFERENCES;
  timeSlots = CONTACT_TIME_SLOTS;

  step: WizardStep = 'details';
  saving = false;
  error = '';

  products: { id: string; name: string }[] = [];
  productChoice = '';

  form = {
    requestType: 'Private Offer',
    marketplace: 'AWS',
    productIds: [] as string[],
    expectedStartDate: '',
    expectedDuration: '',
    estimatedUsers: '',
    businessNeed: '',
    budgetRange: '',
    otherRequirements: '',
    contactPreference: 'Email',
    preferredTimeToContact: ''
  };

  attachmentNames: string[] = [];

  ngOnInit() {
    this.loadProducts();
  }

  loadProducts() {
    this.api.getProducts({ marketplace: this.form.marketplace }).subscribe({
      next: (list: any) => {
        this.products = (list || []).map((p: any) => ({ id: p.id, name: p.name }));
        this.form.productIds = this.form.productIds.filter(id => this.products.some(p => p.id === id));
      },
      error: () => { this.products = []; }
    });
  }

  onMarketplaceChange() {
    this.productChoice = '';
    this.loadProducts();
  }

  addProduct() {
    if (this.productChoice && !this.form.productIds.includes(this.productChoice)) {
      this.form.productIds.push(this.productChoice);
    }
    this.productChoice = '';
  }

  removeProduct(id: string) {
    this.form.productIds = this.form.productIds.filter(p => p !== id);
  }

  get selectedProductNames() {
    return this.form.productIds.map(id => this.productName(id)).join(', ');
  }

  productName(id: string) {
    return this.products.find(p => p.id === id)?.name || id;
  }

  get selectableProducts() {
    return this.products.filter(p => !this.form.productIds.includes(p.id));
  }

  onFilesSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files || []);
    for (const f of files) {
      if (!this.attachmentNames.includes(f.name)) this.attachmentNames.push(f.name);
    }
    input.value = '';
  }

  removeAttachment(name: string) {
    this.attachmentNames = this.attachmentNames.filter(n => n !== name);
  }

  goToReview() {
    this.error = '';
    if (!this.form.requestType || !this.form.marketplace) {
      this.error = 'Please select a request type and marketplace.';
      return;
    }
    this.step = 'review';
  }

  backToDetails() {
    this.step = 'details';
  }

  cancel() {
    void this.router.navigate(['/portal/requests']);
  }

  submit() {
    const user = this.auth.user();
    if (!user) return;
    this.saving = true;
    this.error = '';

    const payload = { ...this.form, attachmentNames: this.attachmentNames };

    this.api.createEngagementRequest(payload, user.email, user.name, user.company || '').subscribe({
      next: () => {
        this.saving = false;
        this.toast.success('Engagement request submitted — our team will be in touch shortly.');
        void this.router.navigate(['/portal/requests']);
      },
      error: () => {
        this.saving = false;
        this.error = 'Could not submit your request. Please try again.';
      }
    });
  }
}
