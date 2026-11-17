import { Component, effect, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UserPreferenceDto } from '../../core/models/api-contracts';

@Component({
  selector: 'app-preferences-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    @if (isOpen()) {
      <div class="modal-backdrop" (click)="onClose()">
        <div class="modal-card" (click)="$event.stopPropagation()" role="dialog" aria-modal="true" aria-labelledby="modal-heading">
          <div class="modal-header">
            <h3 id="modal-heading" class="modal-title">Settings & Preferences</h3>
            <button
              type="button"
              class="close-btn"
              (click)="onClose()"
              aria-label="Close settings modal"
            >
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                <line x1="18" y1="6" x2="6" y2="18"/>
                <line x1="6" y1="6" x2="18" y2="18"/>
              </svg>
            </button>
          </div>

          <!-- Active AI Model Info -->
          <div class="model-info-row">
            <span class="info-label">Active Model</span>
            <span class="model-badge">{{ preferences()?.model || 'gemini-3.5-flash-lite' }}</span>
          </div>

          <form (ngSubmit)="onSave()">
            <!-- Preferred Language -->
            <div class="form-group">
              <label class="form-label" for="pref-lang">Preferred Language</label>
              <select
                id="pref-lang"
                class="form-select"
                [(ngModel)]="selectedLanguage"
                name="selectedLanguage"
                aria-label="Preferred programming language"
              >
                @for (lang of languages; track lang) {
                  <option [value]="lang">{{ lang }}</option>
                }
              </select>
              <span class="form-hint">AI answers and code snippets default to this language.</span>
            </div>

            <!-- Reasoning Level -->
            <div class="form-group">
              <label class="form-label" for="reasoning-level">AI Reasoning Level</label>
              <select
                id="reasoning-level"
                class="form-select"
                [(ngModel)]="reasoningLevel"
                name="reasoningLevel"
                aria-label="AI Reasoning Level"
              >
                <option value="low">Low (Fastest response)</option>
                <option value="medium">Medium (Balanced reasoning)</option>
                <option value="high">High (Deepest analysis)</option>
              </select>
              <span class="form-hint">Controls internal reasoning effort for algorithmic queries.</span>
            </div>

            <div class="form-group-checkbox">
              <label class="checkbox-label">
                <input
                  type="checkbox"
                  class="checkbox-input"
                  [(ngModel)]="autoUnlockL1"
                  name="autoUnlockL1"
                />
                <span>Auto-detect active editor context on tab switch</span>
              </label>
            </div>

            @if (saveSuccess()) {
              <div class="save-toast" role="status">Preferences saved successfully!</div>
            }

            <div class="modal-actions">
              <button
                type="button"
                class="cancel-btn"
                (click)="onClose()"
                aria-label="Close modal without saving"
              >
                Close
              </button>
              <button
                type="submit"
                class="save-btn"
                [disabled]="isSaving()"
                aria-label="Save settings"
              >
                {{ isSaving() ? 'Saving...' : 'Save Settings' }}
              </button>
            </div>
          </form>

          <!-- Privacy & History Section -->
          <div class="privacy-section">
            <div class="privacy-header">
              <h4 class="privacy-title">Data Privacy & History</h4>
              <span class="privacy-badge">Zero Code Persisted</span>
            </div>
            <p class="privacy-desc">
              Your source code and problem descriptions are processed transiently in memory and never stored in any database.
            </p>

            @if (!showClearConfirm()) {
              <button
                type="button"
                class="clear-history-btn"
                (click)="showClearConfirm.set(true)"
                aria-label="Clear all conversation history"
              >
                Clear All Conversation History
              </button>
            } @else {
              <div class="confirm-box" role="alert">
                <span class="confirm-warning">Are you sure? This cannot be undone.</span>
                <div class="confirm-actions">
                  <button
                    type="button"
                    class="confirm-cancel-btn"
                    (click)="showClearConfirm.set(false)"
                    aria-label="Cancel clearing history"
                  >
                    Cancel
                  </button>
                  <button
                    type="button"
                    class="confirm-delete-btn"
                    (click)="onConfirmClearHistory()"
                    aria-label="Confirm clearing all conversation history"
                  >
                    Yes, Clear All
                  </button>
                </div>
              </div>
            }
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    .modal-backdrop {
      position: fixed;
      inset: 0;
      background: rgba(0, 0, 0, 0.65);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1000;
      padding: 16px;
    }
    .modal-card {
      background: var(--color-surface);
      border: 1px solid var(--color-border);
      border-radius: var(--radius-containers);
      width: 100%;
      max-width: 360px;
      padding: 18px;
      box-shadow: var(--shadow-popover);
      animation: modal-fade-in 150ms ease-out;
    }
    @keyframes modal-fade-in {
      from {
        opacity: 0;
        transform: translateY(6px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }
    .modal-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-bottom: 12px;
    }
    .modal-title {
      font-size: var(--text-md);
      font-weight: 600;
      color: var(--color-text);
      margin: 0;
    }
    .close-btn {
      background: transparent;
      border: none;
      color: var(--color-muted);
      cursor: pointer;
      width: 32px;
      height: 32px;
      border-radius: var(--radius-sm);
      display: inline-flex;
      align-items: center;
      justify-content: center;
      transition: all var(--transition-fast);

      &:hover {
        background: var(--color-surface-2);
        color: var(--color-text);
      }
    }
    .model-info-row {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 8px 10px;
      background: var(--color-surface-2);
      border-radius: var(--radius-controls);
      border: 1px solid var(--color-border);
      margin-bottom: 14px;
    }
    .info-label {
      font-size: var(--text-sm);
      color: var(--color-muted);
      font-weight: 500;
    }
    .model-badge {
      font-size: 11px;
      font-family: var(--font-mono);
      padding: 2px 8px;
      border-radius: var(--radius-pill);
      background: var(--color-bg);
      color: var(--color-accent-text);
      border: 1px solid var(--color-border);
    }
    .form-group {
      margin-bottom: 14px;
    }
    .form-label {
      display: block;
      font-size: var(--text-sm);
      font-weight: 500;
      color: var(--color-text);
      margin-bottom: 5px;
    }
    .form-select {
      width: 100%;
      height: 36px;
      box-sizing: border-box;
      background: var(--color-bg);
      border: 1px solid var(--color-border);
      border-radius: var(--radius-controls);
      padding: 0 10px;
      color: var(--color-text);
      font-size: var(--text-sm);
      font-family: inherit;
      transition: border-color var(--transition-fast);

      &:focus {
        outline: none;
        border-color: var(--color-accent);
      }
    }
    .form-hint {
      display: block;
      font-size: var(--text-xs);
      color: var(--color-muted);
      margin-top: 4px;
      line-height: 1.4;
    }
    .form-group-checkbox {
      margin-bottom: 14px;
    }
    .checkbox-label {
      display: flex;
      align-items: center;
      gap: 8px;
      font-size: var(--text-sm);
      color: var(--color-text);
      cursor: pointer;
      user-select: none;
    }
    .checkbox-input {
      accent-color: var(--color-accent);
      width: 16px;
      height: 16px;
    }
    .save-toast {
      font-size: var(--text-xs);
      color: var(--color-success);
      background: rgba(16, 185, 129, 0.1);
      border: 1px solid rgba(16, 185, 129, 0.25);
      padding: 6px 10px;
      border-radius: var(--radius-sm);
      margin-bottom: 12px;
      text-align: center;
    }
    .modal-actions {
      display: flex;
      justify-content: flex-end;
      gap: 8px;
      margin-bottom: 16px;
    }
    .cancel-btn {
      background: var(--color-surface-2);
      color: var(--color-muted);
      border: 1px solid var(--color-border);
      border-radius: var(--radius-controls);
      min-height: 32px;
      padding: 0 12px;
      font-size: var(--text-sm);
      cursor: pointer;
      transition: all var(--transition-fast);

      &:hover {
        color: var(--color-text);
        background: var(--color-border);
      }
    }
    .save-btn {
      background: var(--color-accent);
      color: var(--color-text-on-accent);
      border: none;
      border-radius: var(--radius-controls);
      min-height: 32px;
      padding: 0 14px;
      font-size: var(--text-sm);
      font-weight: 600;
      cursor: pointer;
      transition: background var(--transition-fast);

      &:hover:not(:disabled) {
        background: var(--color-accent-hover);
      }

      &:disabled {
        opacity: 0.45;
        cursor: not-allowed;
      }
    }
    .privacy-section {
      border-top: 1px solid var(--color-border);
      padding-top: 14px;
    }
    .privacy-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-bottom: 6px;
    }
    .privacy-title {
      font-size: 13px;
      font-weight: 600;
      color: var(--color-text);
      margin: 0;
    }
    .privacy-badge {
      font-size: 10px;
      font-weight: 500;
      padding: 2px 6px;
      border-radius: var(--radius-pill);
      background: rgba(16, 185, 129, 0.12);
      color: var(--color-success);
      border: 1px solid rgba(16, 185, 129, 0.25);
    }
    .privacy-desc {
      font-size: var(--text-xs);
      color: var(--color-muted);
      line-height: 1.4;
      margin: 0 0 10px 0;
    }
    .clear-history-btn {
      width: 100%;
      min-height: 32px;
      background: var(--color-error-bg);
      color: var(--color-error-text);
      border: 1px solid var(--color-error-border);
      border-radius: var(--radius-controls);
      padding: 0 12px;
      font-size: var(--text-xs);
      font-weight: 500;
      cursor: pointer;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      transition: opacity var(--transition-fast);

      &:hover {
        opacity: 0.85;
      }
    }
    .confirm-box {
      background: var(--color-error-bg);
      border: 1px solid var(--color-error-border);
      border-radius: var(--radius-controls);
      padding: 10px;
      display: flex;
      flex-direction: column;
      gap: 8px;
    }
    .confirm-warning {
      font-size: var(--text-xs);
      color: var(--color-error-text);
      font-weight: 500;
      text-align: center;
    }
    .confirm-actions {
      display: flex;
      justify-content: center;
      gap: 8px;
    }
    .confirm-cancel-btn {
      background: var(--color-surface-2);
      color: var(--color-text);
      border: 1px solid var(--color-border);
      border-radius: var(--radius-sm);
      min-height: 32px;
      padding: 0 12px;
      font-size: var(--text-xs);
      cursor: pointer;
    }
    .confirm-delete-btn {
      background: var(--color-error-text);
      color: #ffffff;
      border: none;
      border-radius: var(--radius-sm);
      min-height: 32px;
      padding: 0 12px;
      font-size: var(--text-xs);
      font-weight: 600;
      cursor: pointer;
    }
  `]
})
export class PreferencesModalComponent {
  readonly isOpen = input(false);
  readonly preferences = input<UserPreferenceDto | null>(null);

  readonly closed = output<void>();
  readonly preferencesSaved = output<{
    preferredLanguage: string;
    autoUnlockL1: boolean;
    reasoningLevel: string;
  }>();
  readonly clearAllHistory = output<void>();

  readonly isSaving = signal(false);
  readonly saveSuccess = signal(false);
  readonly showClearConfirm = signal(false);

  readonly languages = ['C#', 'Python', 'Java', 'JavaScript', 'TypeScript', 'C++', 'Go', 'Rust'];
  selectedLanguage = 'C#';
  autoUnlockL1 = true;
  reasoningLevel = 'low';

  constructor() {
    effect(() => {
      const pref = this.preferences();
      if (pref) {
        this.selectedLanguage = pref.preferredLanguage || 'C#';
        this.autoUnlockL1 = pref.autoUnlockL1 ?? true;
        this.reasoningLevel = pref.reasoningLevel || 'low';
      }
    });
  }

  onClose(): void {
    this.saveSuccess.set(false);
    this.showClearConfirm.set(false);
    this.closed.emit();
  }

  onConfirmClearHistory(): void {
    this.showClearConfirm.set(false);
    this.clearAllHistory.emit();
  }

  onSave(): void {
    this.isSaving.set(true);
    this.preferencesSaved.emit({
      preferredLanguage: this.selectedLanguage,
      autoUnlockL1: this.autoUnlockL1,
      reasoningLevel: this.reasoningLevel,
    });
    this.isSaving.set(false);
    this.saveSuccess.set(true);
    setTimeout(() => {
      this.saveSuccess.set(false);
    }, 2000);
  }
}
