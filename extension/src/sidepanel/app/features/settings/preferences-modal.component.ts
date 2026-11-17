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
        <div class="modal-card" (click)="$event.stopPropagation()">
          <div class="modal-header">
            <h3 class="modal-title">Settings & Preferences</h3>
            <button type="button" class="close-btn" (click)="onClose()">&times;</button>
          </div>

          <!-- Active AI Model Info -->
          <div class="model-info-row">
            <span class="info-label">Active Model:</span>
            <span class="model-badge">{{ preferences()?.model || 'gemini-2.5-flash' }}</span>
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
              <div class="save-toast">Preferences saved successfully!</div>
            }

            <div class="modal-actions">
              <button type="button" class="cancel-btn" (click)="onClose()">Close</button>
              <button type="submit" class="save-btn" [disabled]="isSaving()">
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
              >
                Clear All Conversation History
              </button>
            } @else {
              <div class="confirm-box">
                <span class="confirm-warning">Are you sure? This cannot be undone.</span>
                <div class="confirm-actions">
                  <button type="button" class="confirm-cancel-btn" (click)="showClearConfirm.set(false)">Cancel</button>
                  <button type="button" class="confirm-delete-btn" (click)="onConfirmClearHistory()">Yes, Clear All</button>
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
      background: rgba(0, 0, 0, 0.7);
      backdrop-filter: blur(2px);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1000;
      padding: 16px;
    }
    .modal-card {
      background: #18181b;
      border: 1px solid #3f3f46;
      border-radius: 8px;
      width: 100%;
      max-width: 360px;
      padding: 16px;
      box-shadow: 0 10px 25px rgba(0, 0, 0, 0.5);
    }
    .modal-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-bottom: 12px;
    }
    .modal-title {
      font-size: 15px;
      font-weight: 600;
      color: #f3f4f6;
      margin: 0;
    }
    .close-btn {
      background: none;
      border: none;
      color: #9ca3af;
      font-size: 20px;
      cursor: pointer;
    }
    .model-info-row {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 8px 10px;
      background: #121215;
      border-radius: 6px;
      border: 1px solid #27272a;
      margin-bottom: 14px;
    }
    .info-label {
      font-size: 12px;
      color: #9ca3af;
    }
    .model-badge {
      font-size: 11px;
      font-family: monospace;
      padding: 2px 8px;
      border-radius: 12px;
      background: rgba(59, 130, 246, 0.15);
      color: #60a5fa;
      border: 1px solid rgba(59, 130, 246, 0.3);
    }
    .form-group {
      margin-bottom: 14px;
    }
    .form-label {
      display: block;
      font-size: 12px;
      font-weight: 500;
      color: #d1d5db;
      margin-bottom: 4px;
    }
    .form-select {
      width: 100%;
      box-sizing: border-box;
      background: #141418;
      border: 1px solid #27272a;
      border-radius: 6px;
      padding: 6px 10px;
      color: #f3f4f6;
      font-size: 13px;
    }
    .form-select:focus {
      outline: none;
      border-color: #3b82f6;
    }
    .form-hint {
      display: block;
      font-size: 11px;
      color: #71717a;
      margin-top: 4px;
    }
    .form-group-checkbox {
      margin-bottom: 14px;
    }
    .checkbox-label {
      display: flex;
      align-items: center;
      gap: 8px;
      font-size: 12px;
      color: #d1d5db;
      cursor: pointer;
    }
    .checkbox-input {
      accent-color: #3b82f6;
    }
    .save-toast {
      font-size: 12px;
      color: #34d399;
      background: rgba(16, 185, 129, 0.15);
      padding: 6px 8px;
      border-radius: 4px;
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
      background: #27272a;
      color: #d1d5db;
      border: none;
      border-radius: 6px;
      padding: 6px 12px;
      font-size: 12px;
      cursor: pointer;
    }
    .save-btn {
      background: #2563eb;
      color: #fff;
      border: none;
      border-radius: 6px;
      padding: 6px 14px;
      font-size: 12px;
      font-weight: 600;
      cursor: pointer;
    }
    .save-btn:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }
    .privacy-section {
      border-top: 1px solid #27272a;
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
      color: #e4e4e7;
      margin: 0;
    }
    .privacy-badge {
      font-size: 10px;
      padding: 1px 6px;
      border-radius: 4px;
      background: rgba(16, 185, 129, 0.15);
      color: #34d399;
      border: 1px solid rgba(16, 185, 129, 0.3);
    }
    .privacy-desc {
      font-size: 11px;
      color: #71717a;
      line-height: 1.4;
      margin: 0 0 10px 0;
    }
    .clear-history-btn {
      width: 100%;
      background: rgba(239, 68, 68, 0.1);
      color: #f87171;
      border: 1px solid rgba(239, 68, 68, 0.25);
      border-radius: 6px;
      padding: 6px 10px;
      font-size: 12px;
      font-weight: 500;
      cursor: pointer;
      transition: background 0.15s, border-color 0.15s;
    }
    .clear-history-btn:hover {
      background: rgba(239, 68, 68, 0.2);
      border-color: rgba(239, 68, 68, 0.4);
    }
    .confirm-box {
      background: rgba(239, 68, 68, 0.08);
      border: 1px solid rgba(239, 68, 68, 0.3);
      border-radius: 6px;
      padding: 8px 10px;
      display: flex;
      flex-direction: column;
      gap: 8px;
    }
    .confirm-warning {
      font-size: 12px;
      color: #fca5a5;
      font-weight: 500;
      text-align: center;
    }
    .confirm-actions {
      display: flex;
      justify-content: center;
      gap: 8px;
    }
    .confirm-cancel-btn {
      background: #27272a;
      color: #d1d5db;
      border: none;
      border-radius: 4px;
      padding: 4px 10px;
      font-size: 11px;
      cursor: pointer;
    }
    .confirm-delete-btn {
      background: #dc2626;
      color: #ffffff;
      border: none;
      border-radius: 4px;
      padding: 4px 12px;
      font-size: 11px;
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
