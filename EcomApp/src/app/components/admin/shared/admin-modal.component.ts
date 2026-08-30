import { Component, input, output, effect } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-admin-modal',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (isOpen()) {
      <div class="modal-overlay" (click)="close.emit()">
        <div class="modal-container" (click)="$event.stopPropagation()">
          <div class="modal-header">
            <h2>{{ title() }}</h2>
            <button class="close-btn" (click)="close.emit()" aria-label="Close modal">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <line x1="18" y1="6" x2="6" y2="18"></line>
                <line x1="6" y1="6" x2="18" y2="18"></line>
              </svg>
            </button>
          </div>
          <div class="modal-body">
            <ng-content></ng-content>
          </div>
          @if (showFooter()) {
            <div class="modal-footer">
              <button class="btn btn-secondary" (click)="close.emit()">{{ cancelText() }}</button>
              <button
                class="btn btn-primary"
                (click)="confirm.emit()"
                [disabled]="loading()">
                @if (loading()) {
                  <span class="spinner"></span>
                }
                {{ confirmText() }}
              </button>
            </div>
          }
        </div>
      </div>
    }
  `,
  styles: [`
    .modal-overlay {
      position: fixed;
      inset: 0;
      background-color: rgba(0, 0, 0, 0.5);
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 1rem;
      z-index: 1000;
      animation: fadeIn 0.2s ease;
    }
    .modal-container {
      background: var(--surface, white);
      border-radius: 12px;
      box-shadow: 0 20px 40px rgba(0, 0, 0, 0.15);
      max-width: 600px;
      width: 100%;
      max-height: 90vh;
      overflow: hidden;
      display: flex;
      flex-direction: column;
      animation: slideUp 0.2s ease;
    }
    .modal-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 1rem 1.5rem;
      border-bottom: 1px solid var(--border-color, #e0e0e0);
    }
    .modal-header h2 {
      margin: 0;
      font-size: 1.25rem;
      font-weight: 600;
    }
    .close-btn {
      background: none;
      border: none;
      cursor: pointer;
      padding: 0.25rem;
      color: var(--on-surface-variant, #666);
      border-radius: 4px;
      transition: background-color 0.2s;
    }
    .close-btn:hover {
      background-color: var(--hover-color, #f0f0f0);
    }
    .modal-body {
      padding: 1.5rem;
      overflow-y: auto;
      flex: 1;
    }
    .modal-footer {
      display: flex;
      justify-content: flex-end;
      gap: 0.75rem;
      padding: 1rem 1.5rem;
      border-top: 1px solid var(--border-color, #e0e0e0);
      background: var(--surface-variant, #fafafa);
    }
    .btn {
      padding: 0.625rem 1.25rem;
      border: none;
      border-radius: 6px;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s;
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
    }
    .btn:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }
    .btn-primary {
      background-color: var(--primary, #2874F0);
      color: white;
    }
    .btn-primary:hover:not(:disabled) {
      background-color: var(--primary-dark, #1a5dc8);
    }
    .btn-secondary {
      background-color: var(--secondary, #6c757d);
      color: white;
    }
    .btn-secondary:hover:not(:disabled) {
      background-color: var(--secondary-dark, #5a6268);
    }
    .spinner {
      width: 16px;
      height: 16px;
      border: 2px solid transparent;
      border-top-color: currentColor;
      border-radius: 50%;
      animation: spin 0.8s linear infinite;
    }
    @keyframes fadeIn {
      from { opacity: 0; }
      to { opacity: 1; }
    }
    @keyframes slideUp {
      from { opacity: 0; transform: translateY(20px); }
      to { opacity: 1; transform: translateY(0); }
    }
    @keyframes spin {
      to { transform: rotate(360deg); }
    }
  `]
})
export class AdminModalComponent {
  isOpen = input.required<boolean>();
  title = input.required<string>();
  showFooter = input(true);
  confirmText = input('Save');
  cancelText = input('Cancel');
  loading = input(false);

  close = output<void>();
  confirm = output<void>();
}