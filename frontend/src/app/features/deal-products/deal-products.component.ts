import { Component, OnInit, inject } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ApiService } from '@core/services/api.service';
import { apiErrorMessage, normalizeDealDetail } from '@shared/utils/deal-api.util';
import { DealStepperComponent } from '@shared/components/deal-stepper/deal-stepper.component';
import { DealFlowFooterComponent } from '@shared/components/deal-flow-footer/deal-flow-footer.component';
import { getEngagementConfig, nextScreenPath, prevScreenPath, screenApplies } from '@shared/utils/engagement.util';

@Component({
  selector: 'app-deal-products',
  standalone: true,
  imports: [DecimalPipe, RouterLink, DealStepperComponent, DealFlowFooterComponent],
  templateUrl: './deal-products.component.html',
  styleUrl: './deal-products.component.scss'
})
export class DealProductsComponent implements OnInit {
  private api = inject(ApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  dealId = '';
  deal: any = null;
  products: any[] = [];
  selected: string[] = [];
  loading = true;
  saving = false;
  error = '';

  get engagementType(): string { return this.deal?.engagementType || 'Private Offer'; }
  get productsOptional(): boolean { return getEngagementConfig(this.engagementType).products === 'optional'; }
  get backPath(): string { return prevScreenPath(this.engagementType, this.dealId, 'products') || `/deals/${this.dealId}/edit`; }

  ngOnInit() {
    this.dealId = this.route.snapshot.paramMap.get('id') || '';
    if (!this.dealId || this.dealId === 'new') {
      this.loading = false;
      this.error = 'Invalid engagement. Create an engagement first.';
      return;
    }

    this.api.getDeal(this.dealId).subscribe({
      next: (raw: any) => {
        const detail = normalizeDealDetail(raw);
        this.deal = detail.deal;
        if (!this.deal) {
          this.error = `Engagement ${this.dealId} was not found. It may have been lost after a backend restart — create a new engagement.`;
          this.loading = false;
          return;
        }
        // Skip this screen if Products doesn't apply to the engagement type.
        if (!screenApplies(this.engagementType, 'products')) {
          const next = nextScreenPath(this.engagementType, this.dealId, 'products') || `/deals/${this.dealId}`;
          this.router.navigateByUrl(next);
          return;
        }
        this.selected = this.deal.productIds || [];
        this.loadProducts();
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        this.error = apiErrorMessage(err, `Engagement ${this.dealId} not found.`);
      }
    });
  }

  loadProducts() {
    const marketplace = this.deal?.marketplace;
    this.api.getProducts(marketplace ? { marketplace } : undefined).subscribe({
      next: (p: any) => {
        this.products = Array.isArray(p) ? p : [];
        if (!this.products.length) {
          this.api.getProducts().subscribe({
            next: (all: any) => this.products = Array.isArray(all) ? all : [],
            error: () => this.error = 'Could not load products.'
          });
        }
      },
      error: () => {
        this.api.getProducts().subscribe({
          next: (all: any) => this.products = Array.isArray(all) ? all : [],
          error: (err) => this.error = apiErrorMessage(err, 'Could not load products.')
        });
      }
    });
  }

  toggle(id: string) {
    this.selected = this.selected.includes(id)
      ? this.selected.filter(x => x !== id)
      : [...this.selected, id];
  }

  private gotoNext() {
    const next = nextScreenPath(this.engagementType, this.dealId, 'products') || `/deals/${this.dealId}`;
    this.router.navigateByUrl(next);
  }

  continue() {
    // Products may be optional (e.g. Workshop) — allow continuing with nothing selected.
    if (!this.selected.length) {
      if (this.productsOptional) { this.gotoNext(); }
      return;
    }
    this.saving = true;
    this.error = '';
    this.api.setProducts(this.dealId, this.selected).subscribe({
      next: () => {
        this.saving = false;
        this.gotoNext();
      },
      error: (err) => {
        this.saving = false;
        this.error = apiErrorMessage(err, 'Could not save product selection.');
      }
    });
  }
}
