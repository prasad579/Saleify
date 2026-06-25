import { Component, ElementRef, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '@core/services/api.service';
import { SnapshotLauncherService } from '@core/services/snapshot-launcher.service';
import { SettingsHintComponent } from '@shared/components/settings-hint/settings-hint.component';
import {
  AttentionRow,
  EmailSummaryResponse,
  EngagementSnapshot,
  PrivateOfferRow,
  SnapshotFieldSetting,
  SnapshotRequest,
  SnapshotSectionSetting
} from '@shared/data/snapshot.model';

/**
 * Global Engagement Snapshot modal. Mounted once in the shell; opened from any
 * surface via SnapshotLauncherService. Renders the five report sections and
 * offers Copy Summary, Download PDF (print), and Email Summary actions.
 */
@Component({
  selector: 'app-engagement-snapshot',
  standalone: true,
  imports: [FormsModule, RouterLink, SettingsHintComponent],
  templateUrl: './engagement-snapshot.component.html',
  styleUrl: './engagement-snapshot.component.scss'
})
export class EngagementSnapshotComponent {
  launcher = inject(SnapshotLauncherService);
  private api = inject(ApiService);
  private host = inject(ElementRef<HTMLElement>);

  snapshot = signal<EngagementSnapshot | null>(null);
  loading = signal(false);
  error = signal('');

  // Email compose state
  showEmail = signal(false);
  emailTo = '';
  emailCc = '';
  emailSubject = '';
  sending = signal(false);
  emailResult = signal<EmailSummaryResponse | null>(null);

  // Transient "copied" feedback
  copiedSummary = signal(false);
  copiedLinkId = signal('');

  constructor() {
    let lastLoaded: SnapshotRequest | null = null;
    effect(() => {
      const open = this.launcher.isOpen();
      const req = this.launcher.request();
      if (open && req && req !== lastLoaded) {
        lastLoaded = req;
        this.load(req);
      }
      if (!open) {
        lastLoaded = null;
      }
    });
  }

  private load(req: SnapshotRequest) {
    this.loading.set(true);
    this.error.set('');
    this.snapshot.set(null);
    this.emailResult.set(null);
    this.showEmail.set(this.launcher.startInEmail());
    this.api.generateSnapshot(req).subscribe({
      next: s => {
        this.snapshot.set(s);
        this.emailSubject = s.suggestedSubject;
        this.loading.set(false);
        if (this.showEmail()) this.scrollEmailIntoView();
      },
      error: () => {
        this.error.set('Could not generate the snapshot. Make sure the API is running.');
        this.loading.set(false);
      }
    });
  }

  close() {
    this.launcher.close();
    this.showEmail.set(false);
    this.sending.set(false);
  }

  // ---- Settings-driven rendering ----
  private section(key: string): SnapshotSectionSetting | undefined {
    return this.snapshot()?.settings?.sections?.find(s => s.key === key);
  }

  sectionOn(key: string): boolean {
    const s = this.section(key);
    return s ? s.enabled : true;
  }

  sectionTitle(key: string, fallback: string): string {
    return this.section(key)?.title || fallback;
  }

  fieldOn(sectionKey: string, fieldKey: string): boolean {
    const s = this.section(sectionKey);
    if (!s) return true;
    const f = s.fields?.find(x => x.key === fieldKey);
    return f ? f.enabled : true;
  }

  fieldLabel(sectionKey: string, fieldKey: string, fallback: string): string {
    const f = this.section(sectionKey)?.fields?.find(x => x.key === fieldKey);
    return f?.label || fallback;
  }

  /** Enabled columns (in configured order) for a table section. */
  cols(sectionKey: string): SnapshotFieldSetting[] {
    return (this.section(sectionKey)?.fields || []).filter(f => f.enabled);
  }

  attentionValue(r: AttentionRow, key: string): string {
    switch (key) {
      case 'customer': return r.customer;
      case 'engagementType': return r.engagementType;
      case 'owner': return r.owner;
      case 'status': return r.status;
      case 'nextActionDate': return r.nextActionDate;
      default: return '';
    }
  }

  offerValueOf(r: PrivateOfferRow, key: string): string {
    switch (key) {
      case 'customer': return r.customer;
      case 'marketplace': return r.marketplace;
      case 'offerValue': return r.offerValue;
      case 'status': return r.status;
      case 'expectedCloseDate': return r.expectedCloseDate;
      default: return '';
    }
  }

  get emailButtonEnabled(): boolean {
    return this.snapshot()?.settings?.emailButtonEnabled ?? true;
  }

  /**
   * Workshop summaries are rendered in the same compact, bullet-list layout the email
   * uses (instead of the metric-card layout) across the on-screen modal and the PDF.
   */
  get isWorkshop(): boolean {
    return this.snapshot()?.engagementType === 'Workshop';
  }

  get emailIntro(): string {
    return this.snapshot()?.settings?.emailIntro ?? '';
  }

  get emailFooter(): string {
    return this.snapshot()?.settings?.emailFooter ?? '';
  }

  toggleEmail() {
    this.showEmail.update(v => !v);
    if (this.showEmail()) this.scrollEmailIntoView();
  }

  private scrollEmailIntoView() {
    setTimeout(() => {
      const el = this.host.nativeElement.querySelector('.snap-email') as HTMLElement | null;
      el?.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    }, 60);
  }

  // ---- Copy summary ----
  copySummary() {
    const s = this.snapshot();
    if (!s) return;
    const text = this.buildPlainText(s);
    navigator.clipboard?.writeText(text).then(() => {
      this.copiedSummary.set(true);
      setTimeout(() => this.copiedSummary.set(false), 2000);
    });
  }

  copyLink(row: { dealId: string; link: string }) {
    navigator.clipboard?.writeText(row.link).then(() => {
      this.copiedLinkId.set(row.dealId);
      setTimeout(() => { if (this.copiedLinkId() === row.dealId) this.copiedLinkId.set(''); }, 2000);
    });
  }

  // ---- Download PDF (printable HTML → browser print dialog) ----
  downloadPdf() {
    const s = this.snapshot();
    if (!s) return;
    const win = window.open('', '_blank');
    if (!win) return;
    win.document.open();
    win.document.write(this.buildPrintableHtml(s));
    win.document.close();
    win.focus();
    setTimeout(() => win.print(), 350);
  }

  // ---- Email summary ----
  sendEmail() {
    const req = this.launcher.request();
    if (!req) return;
    this.sending.set(true);
    this.emailResult.set(null);
    this.api.emailSnapshot({
      ...req,
      to: this.splitAddresses(this.emailTo),
      cc: this.splitAddresses(this.emailCc),
      subject: this.emailSubject?.trim()
    }).subscribe({
      next: res => { this.emailResult.set(res); this.sending.set(false); },
      error: () => {
        this.emailResult.set({ success: false, message: 'Email failed — is the API running?', subject: '', to: [], bodyHtml: '', delivered: false });
        this.sending.set(false);
      }
    });
  }

  private splitAddresses(value: string): string[] {
    return (value || '')
      .split(/[,;\s]+/)
      .map(a => a.trim())
      .filter(Boolean);
  }

  // ---- Renderers ----
  private buildPlainText(s: EngagementSnapshot): string {
    const lines: string[] = [];
    lines.push(s.title);
    lines.push(`Engagement Summary · generated ${s.generatedAt}`);
    lines.push('');
    if (s.event) {
      lines.push('EVENT INFORMATION');
      lines.push(`  ${s.event.name}`);
      lines.push(`  ${s.event.startDate} -> ${s.event.endDate}`);
      lines.push(`  Status: ${s.event.status}`);
      lines.push('');
    }
    lines.push('ENGAGEMENT SUMMARY');
    lines.push(`  Total Engagements: ${s.summary.total}`);
    for (const c of s.summary.byType) lines.push(`  ${c.label}: ${c.count}`);
    lines.push('');
    lines.push('PIPELINE SUMMARY');
    lines.push(`  Expected Pipeline: ${s.pipeline.expectedPipelineDisplay}`);
    lines.push(`  Active Private Offers: ${s.pipeline.activePrivateOffers}`);
    lines.push('');
    if (s.attention.length) {
      lines.push('ENGAGEMENTS REQUIRING ATTENTION');
      for (const r of s.attention)
        lines.push(`  ${r.customer} | ${r.engagementType} | ${r.owner} | ${r.status} | ${r.nextActionDate} | ${r.link}`);
      lines.push('');
    }
    if (s.privateOffers.length) {
      lines.push('ACTIVE PRIVATE OFFERS');
      for (const r of s.privateOffers)
        lines.push(`  ${r.customer} | ${r.marketplace} | ${r.offerValue} | ${r.status} | ${r.expectedCloseDate} | ${r.link}`);
    }
    return lines.join('\n');
  }

  private esc(value: string): string {
    return (value ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
  }

  private buildPrintableHtml(s: EngagementSnapshot): string {
    // Workshop summaries mirror the email layout, which carries the configurable intro/footer lines.
    const isWorkshop = s.engagementType === 'Workshop';
    const introBlock = isWorkshop && s.settings?.emailIntro
      ? `<p class="intro">${this.esc(s.settings.emailIntro)}</p>` : '';
    const footerBlock = isWorkshop && s.settings?.emailFooter
      ? `<p class="footer-note">${this.esc(s.settings.emailFooter)}</p>` : '';

    const eventBlock = s.event ? `
      <h2>Event Information</h2>
      <ul>
        <li><strong>${this.esc(s.event.name)}</strong></li>
        <li>${this.esc(s.event.startDate)} → ${this.esc(s.event.endDate)}</li>
        <li>Status: ${this.esc(s.event.status)}</li>
      </ul>` : '';

    const typeItems = s.summary.byType.map(c => `<li>${this.esc(c.label)}: ${c.count}</li>`).join('');

    const attentionRows = s.attention.map(r => `
      <tr><td>${this.esc(r.customer)}</td><td>${this.esc(r.engagementType)}</td><td>${this.esc(r.owner)}</td>
      <td>${this.esc(r.status)}</td><td>${this.esc(r.nextActionDate)}</td><td><a href="${this.esc(r.link)}">View</a></td></tr>`).join('');

    const offerRows = s.privateOffers.map(r => `
      <tr><td>${this.esc(r.customer)}</td><td>${this.esc(r.marketplace)}</td><td>${this.esc(r.offerValue)}</td>
      <td>${this.esc(r.status)}</td><td>${this.esc(r.expectedCloseDate)}</td><td><a href="${this.esc(r.link)}">View</a></td></tr>`).join('');

    const attentionBlock = s.attention.length ? `
      <h2>Engagements Requiring Attention</h2>
      <table><thead><tr><th>Customer</th><th>Type</th><th>Owner</th><th>Status</th><th>Next Action</th><th>Link</th></tr></thead>
      <tbody>${attentionRows}</tbody></table>` : '';

    const offerBlock = s.privateOffers.length ? `
      <h2>Active Private Offers</h2>
      <table><thead><tr><th>Customer</th><th>Marketplace</th><th>Offer Value</th><th>Status</th><th>Expected Close</th><th>Link</th></tr></thead>
      <tbody>${offerRows}</tbody></table>` : '';

    return `<!doctype html><html><head><meta charset="utf-8"><title>${this.esc(s.suggestedSubject)}</title>
    <style>
      body { font-family: Inter, Segoe UI, Arial, sans-serif; color:#0f172a; margin:32px; }
      h1 { margin:0 0 4px; font-size:22px; }
      h2 { margin:20px 0 8px; font-size:15px; color:#3730a3; border-bottom:1px solid #e2e8f0; padding-bottom:4px; }
      .meta { color:#64748b; font-size:12px; margin:0 0 8px; }
      .intro { margin:0 0 14px; }
      .footer-note { margin:16px 0 0; color:#64748b; font-size:12px; }
      ul { margin:0; padding-left:18px; }
      table { border-collapse:collapse; width:100%; font-size:12px; margin-top:4px; }
      th { text-align:left; border-bottom:2px solid #e2e8f0; padding:6px 8px; }
      td { border-bottom:1px solid #f1f5f9; padding:6px 8px; }
      a { color:#4f46e5; }
      @media print { body { margin:12mm; } a { color:#0f172a; text-decoration:none; } }
    </style></head><body>
      <h1>${this.esc(s.title)}</h1>
      <p class="meta">Engagement Summary · generated ${this.esc(s.generatedAt)}</p>
      ${introBlock}
      ${eventBlock}
      <h2>Engagement Summary</h2>
      <ul><li><strong>Total Engagements: ${s.summary.total}</strong></li>${typeItems}</ul>
      <h2>Pipeline Summary</h2>
      <ul><li>Expected Pipeline: <strong>${this.esc(s.pipeline.expectedPipelineDisplay)}</strong></li>
      <li>Active Private Offers: ${s.pipeline.activePrivateOffers}</li></ul>
      ${attentionBlock}
      ${offerBlock}
      ${footerBlock}
    </body></html>`;
  }
}
