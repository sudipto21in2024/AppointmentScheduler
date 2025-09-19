import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Reusable modal component for displaying dialogs or temporary content.
 *
 * @example
 * ```html
 * <app-modal [isOpen]="isModalOpen" title="Confirmation" (close)="closeModal()">
 *   <p>Are you sure you want to perform this action?</p>
 *   <app-button label="Confirm" (clicked)="confirmAction()"></app-button>
 * </app-modal>
 * ```
 *
 * @property {boolean} isOpen - Controls the visibility of the modal. Defaults to 'false'.
 * @property {string} title - The title displayed in the modal header.
 * @property {boolean} showCloseButton - Whether to display a close button in the header. Defaults to 'true'.
 * @property {boolean} closeOnOverlayClick - Whether clicking the overlay closes the modal. Defaults to 'true'.
 * @property {EventEmitter<void>} close - Event emitted when the modal is closed.
 */

@Component({
  selector: 'app-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './modal.component.html',
  styleUrls: ['./modal.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ModalComponent {
  @Input() isOpen: boolean = false;
  @Input() title: string = '';
  @Input() showCloseButton: boolean = true;
  @Input() closeOnOverlayClick: boolean = true;

  @Output() close = new EventEmitter<void>();

  onClose(): void {
    this.close.emit();
  }

  onOverlayClick(): void {
    if (this.closeOnOverlayClick) {
      this.onClose();
    }
  }
}