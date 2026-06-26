import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '@core/services/api.service';
import { ConfirmDialogService } from '@core/services/confirm-dialog.service';
import { ToastService } from '@core/services/toast.service';
import { apiErrorMessage } from '@shared/utils/deal-api.util';
import { Person } from '@shared/data/lookups';
import { ENGAGEMENT_CONFIGS } from '@shared/utils/engagement.util';

interface PersonForm {
  name: string;
  email: string;
  role: string;
  enabled: boolean;
  engagementTypes: string[];
  source: string;
}

@Component({
  selector: 'app-people-settings',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './people-settings.component.html',
  styleUrl: './people-settings.component.scss'
})
export class PeopleSettingsComponent implements OnInit {
  private api = inject(ApiService);
  private confirm = inject(ConfirmDialogService);
  private toast = inject(ToastService);

  people: Person[] = [];
  engagementTypes = ENGAGEMENT_CONFIGS.map(c => c.type);
  roleSuggestions = ['Deal Desk', 'Sales', 'Partner', 'Solutions Engineer', 'Sales Manager'];

  search = '';
  loading = true;
  error = '';
  success = '';

  // Add / edit modal state
  showModal = false;
  editId = '';
  form: PersonForm = this.emptyForm();

  ngOnInit() { this.load(); }

  get filteredPeople(): Person[] {
    const q = this.search.trim().toLowerCase();
    if (!q) return this.people;
    return this.people.filter(p =>
      (p.name || '').toLowerCase().includes(q) ||
      (p.email || '').toLowerCase().includes(q) ||
      (p.role || '').toLowerCase().includes(q) ||
      (p.engagementTypes || []).some(t => t.toLowerCase().includes(q))
    );
  }

  /** Role dropdown options — suggestions plus the current value if it's a custom one. */
  get roleOptions(): string[] {
    const r = this.form.role.trim();
    return r && !this.roleSuggestions.includes(r) ? [r, ...this.roleSuggestions] : this.roleSuggestions;
  }

  load() {
    this.loading = true;
    this.api.getPeople().subscribe({
      next: (data) => {
        this.people = Array.isArray(data) ? data : [];
        this.loading = false;
      },
      error: (err) => {
        this.error = apiErrorMessage(err, 'Could not load people.');
        this.loading = false;
      }
    });
  }

  private emptyForm(): PersonForm {
    return { name: '', email: '', role: '', enabled: true, engagementTypes: [], source: 'manual' };
  }

  openAdd() {
    this.error = '';
    this.editId = '';
    this.form = this.emptyForm();
    this.showModal = true;
  }

  openEdit(p: Person) {
    this.error = '';
    this.editId = p.id;
    this.form = {
      name: p.name,
      email: p.email,
      role: p.role,
      enabled: p.enabled,
      engagementTypes: [...(p.engagementTypes ?? [])],
      source: p.source || 'manual'
    };
    this.showModal = true;
  }

  closeModal() { this.showModal = false; this.editId = ''; }

  hasType(type: string): boolean {
    return this.form.engagementTypes.includes(type);
  }

  toggleType(type: string): void {
    this.form.engagementTypes = this.hasType(type)
      ? this.form.engagementTypes.filter(t => t !== type)
      : [...this.form.engagementTypes, type];
  }

  save() {
    this.error = '';
    if (!this.form.name.trim()) { this.toast.error('Name is required.'); return; }

    const payload: Partial<Person> = {
      id: this.editId || undefined,
      name: this.form.name.trim(),
      email: this.form.email.trim(),
      role: this.form.role.trim(),
      enabled: this.form.enabled,
      engagementTypes: this.form.engagementTypes,
      source: this.form.source || 'manual'
    };

    const editing = !!this.editId;
    this.api.savePerson(payload).subscribe({
      next: () => {
        this.toast.success(editing ? 'Person updated.' : 'Person added.');
        this.closeModal();
        this.load();
      },
      error: (err) => { this.toast.error(apiErrorMessage(err, 'Could not save person.')); }
    });
  }

  toggleEnabled(p: Person) {
    this.error = '';
    this.api.togglePerson(p.id).subscribe({
      next: (updated) => {
        p.enabled = updated.enabled;
        this.toast.success(`${p.name} ${p.enabled ? 'enabled' : 'disabled'} for engagements.`);
      },
      error: (err) => { this.toast.error(apiErrorMessage(err, 'Could not update the person.')); }
    });
  }

  async remove(p: Person) {
    const ok = await this.confirm.open({
      title: 'Delete person?',
      lines: [`Remove ${p.name} from the engagement owners list?`, 'This cannot be undone.'],
      confirmLabel: 'Delete',
      danger: true
    });
    if (!ok) return;
    this.error = '';
    this.api.deletePerson(p.id).subscribe({
      next: () => { this.toast.success(`${p.name} deleted.`); this.load(); },
      error: (err) => { this.toast.error(apiErrorMessage(err, 'Could not delete person.')); }
    });
  }

  async resetAll() {
    const ok = await this.confirm.open({
      title: 'Reset people?',
      lines: ['Reset the people list back to the default owners?', 'Any people you added or edited will be lost.'],
      confirmLabel: 'Reset',
      danger: true
    });
    if (!ok) return;
    this.error = '';
    this.api.resetPeople().subscribe({
      next: (data) => {
        this.people = Array.isArray(data) ? data : [];
        this.toast.success('People reset to defaults.');
      },
      error: (err) => { this.toast.error(apiErrorMessage(err, 'Could not reset people.')); }
    });
  }
}
