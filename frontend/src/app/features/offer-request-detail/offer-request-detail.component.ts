import { Component, OnInit, inject } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ApiService } from '@core/services/api.service';
import { ToastService } from '@core/services/toast.service';
import { OfferRequest } from '@shared/data/offer-request.model';
import { offerStatusBadge, offerResponseBadge } from '@shared/utils/offer-request.util';
import { apiErrorMessage } from '@shared/utils/deal-api.util';
import { formatCreatedDate } from '@shared/utils/pagination.util';

@Component({
  selector: 'app-offer-request-detail',
  standalone: true,
  imports: [FormsModule, RouterLink, DecimalPipe],
  templateUrl: './offer-request-detail.component.html',
  styleUrl: './offer-request-detail.component.scss'
})
export class OfferRequestDetailComponent implements OnInit {
  private api = inject(ApiService);
  private toast = inject(ToastService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  offer: OfferRequest | null = null;
  loading = true;
  error = '';

  jsonTab: 'request' | 'response' = 'request';
  formatCreatedDate = formatCreatedDate;
  statusBadge = offerStatusBadge;
  responseBadge = offerResponseBadge;

  // Capture-response modal
  showResponse = false;
  respForm = { status: 'Accepted', reference: '', notes: '', json: '' };
  savingResponse = false;
  readonly responseStatuses = ['Accepted', 'Rejected', 'Changes Requested', 'Pending'];

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id') || '';
    this.load(id);
  }

  load(id: string) {
    this.loading = true;
    this.api.getOfferRequest(id).subscribe({
      next: o => { this.offer = o; this.loading = false; },
      error: e => {
        this.loading = false;
        this.error = e?.status === 404 ? 'Offer request not found.' : apiErrorMessage(e, 'Could not load the offer request.');
      }
    });
  }

  get currentJson(): string {
    if (!this.offer) return '';
    return this.jsonTab === 'response' ? this.offer.responseJson : this.offer.requestJson;
  }

  copyJson() {
    navigator.clipboard?.writeText(this.currentJson).then(
      () => this.toast.success('JSON copied to clipboard.'),
      () => this.toast.error('Could not copy to clipboard.')
    );
  }

  downloadJson() {
    if (!this.offer) return;
    const name = `${this.offer.id}-${this.jsonTab}.json`;
    const blob = new Blob([this.currentJson], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = name;
    a.click();
    URL.revokeObjectURL(url);
  }

  openResponse() {
    if (!this.offer) return;
    this.respForm = {
      status: this.offer.responseReceived ? this.offer.responseStatus : 'Accepted',
      reference: this.offer.responseReference || '',
      notes: this.offer.responseNotes || '',
      json: this.offer.responseJson || ''
    };
    this.showResponse = true;
  }

  closeResponse() { this.showResponse = false; }

  saveResponse() {
    if (!this.offer) return;
    this.savingResponse = true;
    this.api.captureOfferResponse(this.offer.id, {
      status: this.respForm.status,
      reference: this.respForm.reference,
      notes: this.respForm.notes,
      json: this.respForm.json
    }).subscribe({
      next: updated => {
        this.offer = updated;
        this.savingResponse = false;
        this.showResponse = false;
        this.jsonTab = 'response';
        this.toast.success(`Response captured — ${updated.responseStatus}.`);
      },
      error: e => { this.savingResponse = false; this.toast.error(apiErrorMessage(e, 'Could not capture the response.')); }
    });
  }
}
