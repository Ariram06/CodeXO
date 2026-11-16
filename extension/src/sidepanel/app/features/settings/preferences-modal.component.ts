import { Component, input, output, signal } from '@angular/core';
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
            <h3 class="modal-title">Study Preferences</h3>
            <button type="button" class="close-btn" (click)="onClose()">&times;</button>
          </div>

          <form (ngSubmit)="onSave()">
            <div class="form-group">
              <label class="form-label" for="pref-lang">Preferred Language for Examples</label>
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
              <span class="form-hint">Hints use this language when illustrating syntax-free concepts.</span>
            </div>

            <div class="form-group-checkbox">
              <label class="checkbox-label">
                <input
                  type="checkbox"
                  class="checkbox-input"
                  [(ngModel)]="autoUnlockL1"
                  name="autoUnlockL1"
                />
                <span>Automatically unlock L1 (Understand) when problem loads</span>
              </label>
            </div>

            @if (saveSuccess()) {
              <div class="save-toast">Preferences saved successfully!</div>
            }

            <div class="modal-actions">
              <button type="button" class="cancel-btn" (click)="onClose()">Close</button>
              <button type="submit" class="save-btn" [disabled]="isSaving()">
                {{ isSaving() ? 'Saving...' : 'Save Preferences' }}
              </button>
            </div>
          </form>
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
      max-width: 340px;
      padding: 16px;
      box-shadow: 0 10px 25px rgba(0, 0, 0, 0.5);
    }
    .modal-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-bottom: 14px;
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
  `]
})
export class PreferencesModalComponent {
  readonly isOpen = input(false);
  readonly preferences = input<UserPreferenceDto | null>(null);

  readonly closed = output<void>();
  readonly preferencesSaved = output<{
    preferredLanguage: string;
    autoUnlockL1: boolean;
  }>();

  readonly isSaving = signal(false);
  readonly saveSuccess = signal(false);

  readonly languages = ['C#', 'Python', 'Java', 'JavaScript', 'TypeScript', 'C++', 'Go', 'Rust'];
  selectedLanguage = 'C#';
  autoUnlockL1 = true;

  onClose(): void {
    this.saveSuccess.set(false);
    this.closed.emit();
  }

  onSave(): void {
    this.isSaving.set(true);
    this.preferencesSaved.emit({
      preferredLanguage: this.selectedLanguage,
      autoUnlockL1: this.autoUnlockL1,
    });
    this.isSaving.set(false);
    this.saveSuccess.set(true);
    setTimeout(() => {
      this.saveSuccess.set(false);
    }, 2000);
  }
}
