import { Component, ElementRef, HostListener, Input, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

/**
 * Small ⓘ info icon that opens a popover explaining where a piece of data is
 * customized, with a link to the relevant Settings sub-page.
 * Usage: <app-settings-hint text="…" link="/settings/campaign-events" linkLabel="Open Campaign Tags" />
 */
@Component({
  selector: 'app-settings-hint',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './settings-hint.component.html',
  styleUrl: './settings-hint.component.scss'
})
export class SettingsHintComponent {
  @Input() text = '';
  @Input() link = '';
  @Input() linkLabel = 'Open settings';
  open = signal(false);
  private host = inject(ElementRef<HTMLElement>);

  toggle(event: MouseEvent) {
    event.stopPropagation();
    event.preventDefault();
    this.open.update(v => !v);
  }

  close() { this.open.set(false); }

  @HostListener('document:click', ['$event'])
  onDocClick(e: MouseEvent) {
    if (this.open() && !this.host.nativeElement.contains(e.target as Node)) this.close();
  }
}
