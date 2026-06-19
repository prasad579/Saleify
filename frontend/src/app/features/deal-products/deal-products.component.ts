import { Component, OnInit, inject } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ApiService } from '@core/services/api.service';
import { apiErrorMessage, normalizeDealDetail } from '@shared/utils/deal-api.util';
import { DealStepperComponent } from '@shared/components/deal-stepper/deal-stepper.component';
import { DealFlowFooterComponent } from '@shared/components/deal-flow-footer/deal-flow-footer.component';

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

  ngOnInit() {
    this.dealId = this.route.snapshot.paramMap.get('id') || '';
    if (!this.dealId || this.dealId === 'new') {
      this.loading = false;
      this.error = 'Invalid deal. Create a deal first.';
      return;
    }

    this.api.getDeal(this.dealId).subscribe({
      next: (raw: any) => {
        const detail = normalizeDealDetail(raw);
        this.deal = detail.deal;
        if (!this.deal) {
          this.error = `Deal ${this.dealId} was not found. It may have been lost after a backend restart — create a new deal.`;
          this.loading = false;
          return;
        }
        this.selected = this.deal.productIds || [];
        this.loadProducts();
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        this.error = apiErrorMessage(err, `Deal ${this.dealId} not found.`);
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

  continue() {
    if (!this.selected.length) return;
    this.saving = true;
    this.error = '';
    this.api.setProducts(this.dealId, this.selected).subscribe({
      next: () => {
        this.saving = false;
        this.router.navigate(['/deals', this.dealId, 'pricing']);
      },
      error: (err) => {
        this.saving = false;
        this.error = apiErrorMessage(err, 'Could not save product selection.');
      }
    });
  }
}
