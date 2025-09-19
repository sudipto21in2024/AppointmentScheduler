import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common'; // Import CommonModule for ngIf
import { MatIconModule } from '@angular/material/icon'; // Import MatIconModule for mat-icon

/**
 * Reusable button component.
 *
 * @example
 * ```html
 * <app-button label="Click Me" (clicked)="onClick()"></app-button>
 * <app-button variant="secondary" size="small" icon="add">Add Item</app-button>
 * <app-button variant="warn" [disabled]="true">Disabled Button</app-button>
 * ```
 *
 * @property {string} label - The text content of the button.
 * @property {'button' | 'submit' | 'reset'} type - The HTML button type. Defaults to 'button'.
 * @property {'primary' | 'secondary' | 'accent' | 'warn' | 'success' | 'text'} variant - Visual style of the button. Defaults to 'primary'.
 * @property {'small' | 'medium' | 'large'} size - Size of the button. Defaults to 'medium'.
 * @property {boolean} disabled - Whether the button is disabled. Defaults to 'false'.
 * @property {boolean} loading - Whether the button is in a loading state. Defaults to 'false'.
 * @property {boolean} fullWidth - Whether the button should take full width. Defaults to 'false'.
 * @property {string} icon - Material icon name to display within the button.
 * @property {EventEmitter<Event>} clicked - Event emitted when the button is clicked.
 */

@Component({
  selector: 'app-button',
  standalone: true,
  imports: [CommonModule, MatIconModule],
  templateUrl: './button.component.html',
  styleUrls: ['./button.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ButtonComponent {
  @Input() label: string = '';
  @Input() type: 'button' | 'submit' | 'reset' = 'button';
  @Input() variant: 'primary' | 'secondary' | 'accent' | 'warn' | 'success' | 'text' = 'primary';
  @Input() size: 'small' | 'medium' | 'large' = 'medium';
  @Input() disabled: boolean = false;
  @Input() loading: boolean = false;
  @Input() fullWidth: boolean = false;
  @Input() icon: string = ''; // Material icon name

  @Output() clicked = new EventEmitter<Event>();

  onClick(event: Event): void {
    if (!this.disabled && !this.loading) {
      this.clicked.emit(event);
    }
  }

  get buttonClasses(): Record<string, boolean> {
    return {
      'app-button': true,
      [`app-button--${this.variant}`]: true,
      [`app-button--${this.size}`]: true,
      'app-button--full-width': this.fullWidth,
      'app-button--disabled': this.disabled || this.loading,
      'app-button--loading': this.loading,
    };
  }
}