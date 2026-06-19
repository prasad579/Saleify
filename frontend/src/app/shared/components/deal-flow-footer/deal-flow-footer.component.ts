import { Component, EventEmitter, Input, Output } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-deal-flow-footer',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './deal-flow-footer.component.html',
  styleUrl: './deal-flow-footer.component.scss'
})
export class DealFlowFooterComponent {
  @Input() hint = '';
  @Input() backLink = '';
  @Input() backLabel = '← Back';
  @Input() exitLink = '';
  @Input() exitLabel = 'Save & Exit';
  @Input() saveLabel = 'Save';
  @Input() continueLabel = '';
  @Input() saving = false;
  @Input() showSave = false;
  @Input() continueDisabled = false;
  @Output() save = new EventEmitter<void>();
  @Output() proceed = new EventEmitter<void>();
}
