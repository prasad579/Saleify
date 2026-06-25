import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { ApiService } from '@core/services/api.service';
import { DealStepperComponent } from '@shared/components/deal-stepper/deal-stepper.component';
import { DealFlowFooterComponent } from '@shared/components/deal-flow-footer/deal-flow-footer.component';
import { paginateSlice, totalPages } from '@shared/utils/pagination.util';
import { apiErrorMessage } from '@shared/utils/deal-api.util';
import { getEngagementConfig, screenApplies } from '@shared/utils/engagement.util';

type ApprovalAuditRow = {
  id: string;
  timestamp: string;
  category: string;
  user: string;
  action: string;
  details: string;
  stepTitle: string;
  documentName: string;
};

type ApprovalStep = {
  id: string;
  order: number;
  title: string;
  assignee: string;
  status: string;
  ruleReason: string;
  completedAt?: string;
  comment?: string;
  comments: { id: string; author: string; text: string; timestamp: string; type: string }[];
};

type ApprovalDoc = {
  id: string;
  name: string;
  documentType: string;
  version: string;
  generatedAt: string;
  fileName: string;
  stale: boolean;
  locked: boolean;
  fillPercent: number;
};

@Component({
  selector: 'app-deal-approvals',
  standalone: true,
  imports: [FormsModule, RouterLink, DecimalPipe, DealStepperComponent, DealFlowFooterComponent],
  templateUrl: './deal-approvals.component.html',
  styleUrl: './deal-approvals.component.scss'
})
export class DealApprovalsComponent implements OnInit {
  private api = inject(ApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private sanitizer = inject(DomSanitizer);

  dealId = '';
  deal: any = null;
  approvals: any = null;
  loading = true;
  saving = false;
  error = '';
  success = '';

  activeStepId = '';
  stepComments: Record<string, string> = {};
  auditPage = 1;
  auditCategory = 'All';
  readonly auditPageSize = 5;
  readonly auditCategories = ['All', 'Approval', 'Document', 'System'];

  previewDoc: ApprovalDoc | null = null;
  previewUrl: SafeResourceUrl | null = null;

  get engagementType(): string { return this.deal?.engagementType || 'Private Offer'; }
  get submitLabel(): string { return getEngagementConfig(this.engagementType).submitLabel; }

  ngOnInit() {
    this.dealId = this.route.snapshot.paramMap.get('id') || '';
    this.load();
  }

  load() {
    this.loading = true;
    this.error = '';
    this.api.getApprovals(this.dealId).subscribe({
      next: (res: any) => {
        this.applyResponse(res);
        this.loading = false;
      },
      error: (err) => {
        if (err?.status === 0 || err?.status === 404) {
          this.bootstrapApprovals(err);
          return;
        }
        this.error = apiErrorMessage(err, 'Could not load approvals.');
        this.loading = false;
      }
    });
  }

  private bootstrapApprovals(cause?: any) {
    this.api.enterApprovals(this.dealId).subscribe({
      next: (res: any) => {
        this.applyResponse(res);
        this.loading = false;
        this.error = '';
      },
      error: () => {
        this.api.getApprovals(this.dealId).subscribe({
          next: (res: any) => {
            this.applyResponse(res);
            this.loading = false;
            this.error = '';
          },
          error: (err) => {
            this.loading = false;
            this.error = apiErrorMessage(cause ?? err, 'Could not load approvals.');
          }
        });
      }
    });
  }

  private buildSummaryFromDeal(data: any) {
    const steps = data.steps ?? [];
    const required = steps.filter((s: ApprovalStep) => s.id !== 'eula');
    const approved = required.filter((s: ApprovalStep) => s.status === 'Approved').length;
    const total = required.length;
    return {
      ...data,
      nextStep: required.find((s: ApprovalStep) => s.status !== 'Approved')?.title ?? 'Complete',
      progress: {
        total,
        approved,
        pending: required.filter((s: ApprovalStep) => s.status === 'Pending').length,
        changesRequested: required.filter((s: ApprovalStep) =>
          s.status === 'Changes Requested' || s.status === 'Needs Re-approval').length,
        rejected: required.filter((s: ApprovalStep) => s.status === 'Rejected').length,
        percent: total ? Math.round(approved * 100 / total) : 0
      },
      eulaDocument: data.documents?.find((d: ApprovalDoc) => d.documentType === 'eula'),
      packageDocument: data.documents?.find((d: ApprovalDoc) => d.id === data.packageSummaryId)
    };
  }

  private applyResponse(res: any) {
    const detail = res.deal;
    this.deal = detail?.deal ?? detail;
    // Approvals don't apply to every engagement type — bounce to the overview if so.
    if (this.deal && !screenApplies(this.deal.engagementType || 'Private Offer', 'approvals')) {
      this.router.navigate(['/deals', this.dealId]);
      return;
    }
    this.approvals = res.approvals ?? this.buildSummaryFromDeal(this.deal?.approvals);
    if (!this.approvals) {
      this.error = 'No approval data returned from the server.';
      return;
    }
    if (!this.activeStepId && this.approvals?.steps?.length) {
      const next = this.approvals.steps.find((s: ApprovalStep) =>
        s.status !== 'Approved' && s.id !== 'eula');
      this.activeStepId = next?.id ?? this.approvals.steps[0].id;
    }
  }

  get detailDocuments(): ApprovalDoc[] {
    return (this.approvals?.documents ?? []).filter((d: ApprovalDoc) => d.documentType !== 'package' && d.documentType !== 'eula');
  }

  get filteredAudit(): ApprovalAuditRow[] {
    const log: ApprovalAuditRow[] = this.approvals?.auditLog ?? [];
    if (this.auditCategory === 'All') return log;
    return log.filter(r => (r.category || 'Approval') === this.auditCategory);
  }

  get pagedAudit(): ApprovalAuditRow[] {
    return paginateSlice(this.filteredAudit, this.auditPage, this.auditPageSize);
  }

  get auditTotalPages() {
    return totalPages(this.filteredAudit.length, this.auditPageSize);
  }

  selectStep(id: string) {
    this.activeStepId = id;
  }

  statusClass(status: string): string {
    switch (status) {
      case 'Approved': return 'badge-green';
      case 'Rejected': return 'badge-orange';
      case 'Changes Requested':
      case 'Needs Re-approval': return 'badge-orange';
      case 'Pending': return 'badge-gray';
      default: return 'badge-purple';
    }
  }

  auditCategoryClass(cat: string): string {
    switch (cat) {
      case 'Document': return 'badge-purple';
      case 'System': return 'badge-gray';
      default: return 'badge-blue';
    }
  }

  canActOnStep(step: ApprovalStep): boolean {
    return step.id !== 'eula' && step.status !== 'Approved' && !this.approvals?.documentsLocked;
  }

  applyActionForStep(stepId: string, action: string) {
    const comment = (this.stepComments[stepId] || '').trim();
    if ((action === 'reject' || action === 'request-changes') && !comment) {
      this.error = 'Please enter a comment before requesting changes or rejecting.';
      this.activeStepId = stepId;
      return;
    }
    this.saving = true;
    this.error = '';
    this.api.approvalAction(this.dealId, { stepId, action, comment }).subscribe({
      next: (res: any) => {
        this.approvals = res.approvals;
        this.saving = false;
        this.stepComments[stepId] = '';
        this.success = res.message;
        setTimeout(() => this.success = '', 3000);
        this.load();
      },
      error: (err) => {
        this.saving = false;
        this.error = err?.error?.message || apiErrorMessage(err, 'Action failed.');
      }
    });
  }

  viewDocument(doc: ApprovalDoc) {
    this.previewDoc = doc;
    const url = this.api.documentViewUrl(this.dealId, doc.id);
    this.previewUrl = this.sanitizer.bypassSecurityTrustResourceUrl(url);
  }

  closePreview() {
    this.previewDoc = null;
    this.previewUrl = null;
    this.load();
  }

  downloadDocument(doc: ApprovalDoc) {
    window.open(this.api.documentDownloadUrl(this.dealId, doc.id), '_blank');
    setTimeout(() => this.load(), 500);
  }

  regenerateDocs() {
    if (this.approvals?.documentsLocked) {
      this.error = 'Documents are locked. Unlock for edits first, or change pricing to trigger re-approval.';
      return;
    }
    this.saving = true;
    this.api.regenerateApprovalDocuments(this.dealId).subscribe({
      next: (res: any) => {
        this.approvals = res.approvals;
        this.saving = false;
        this.success = 'Documents regenerated.';
        setTimeout(() => this.success = '', 3000);
      },
      error: (err) => {
        this.saving = false;
        this.error = apiErrorMessage(err, 'Could not regenerate documents.');
      }
    });
  }

  unlockForEdits() {
    this.saving = true;
    this.api.unlockApprovals(this.dealId).subscribe({
      next: (res: any) => {
        this.approvals = res.approvals;
        this.saving = false;
        this.success = 'Documents unlocked — you can edit pricing/products. Re-approval required after changes.';
        setTimeout(() => this.success = '', 4000);
      },
      error: (err) => {
        this.saving = false;
        this.error = err?.error?.message || apiErrorMessage(err, 'Could not unlock documents.');
      }
    });
  }

  submitDeal() {
    this.saving = true;
    this.error = '';
    // Status outcome depends on engagement type (Submit to SaaSify / Mark Completed).
    this.api.submitEngagement(this.dealId).subscribe({
      next: () => {
        this.saving = false;
        this.router.navigate(['/deals', this.dealId]);
      },
      error: (err) => {
        this.saving = false;
        this.error = err?.error?.message || apiErrorMessage(err, 'Cannot submit yet.');
      }
    });
  }

  onAuditFilterChange() {
    this.auditPage = 1;
  }

  prevAuditPage() {
    if (this.auditPage > 1) this.auditPage--;
  }

  nextAuditPage() {
    if (this.auditPage < this.auditTotalPages) this.auditPage++;
  }
}
