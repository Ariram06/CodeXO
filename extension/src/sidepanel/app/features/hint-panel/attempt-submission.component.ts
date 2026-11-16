import { Component, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SubmitAttemptRequest } from '../../core/models/api-contracts';

@Component({
  selector: 'app-attempt-submission',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="attempt-card">
      <div class="card-header">
        <h3 class="card-title">Unlock Next Hint</h3>
        <span class="required-badge">Attempt Required</span>
      </div>
      <p class="card-prompt">
        What have you understood or tried so far? Write at least one line to unlock the next level.
      </p>

      <form (ngSubmit)="onSubmit()">
        <div class="form-group">
          <textarea
            class="attempt-textarea"
            rows="3"
            placeholder="e.g., I realized a hash map can give O(1) lookup for complements, but I'm unsure how to handle duplicate elements..."
            [(ngModel)]="attemptContent"
            name="attemptContent"
            (ngModelChange)="onContentChange($event)"
            [disabled]="isSubmitting"
          ></textarea>
        </div>

        @if (validationError()) {
          <div class="validation-message" [ngClass]="isWarning ? 'warning-msg' : 'error-msg'">
            {{ validationError() }}
          </div>
        }

        <div class="form-actions">
          <button
            type="submit"
            class="submit-button"
            [disabled]="!isValid() || isSubmitting"
          >
            @if (isSubmitting) {
              <span>Verifying Attempt...</span>
            } @else {
              <span>Submit Attempt &rarr;</span>
            }
          </button>
        </div>
      </form>
    </div>
  `,
  styles: [`
    .attempt-card {
      background: #1e1e24;
      border: 1px solid #3b82f6;
      border-radius: 8px;
      padding: 14px;
      margin-top: 14px;
    }
    .card-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-bottom: 6px;
    }
    .card-title {
      font-size: 14px;
      font-weight: 600;
      color: #93c5fd;
      margin: 0;
    }
    .required-badge {
      font-size: 10px;
      font-weight: 700;
      text-transform: uppercase;
      padding: 2px 6px;
      background: rgba(59, 130, 246, 0.2);
      color: #60a5fa;
      border-radius: 4px;
    }
    .card-prompt {
      font-size: 12px;
      color: #9ca3af;
      margin: 0 0 10px 0;
      line-height: 1.4;
    }
    .attempt-textarea {
      width: 100%;
      box-sizing: border-box;
      background: #141418;
      border: 1px solid #2e2e38;
      border-radius: 6px;
      padding: 8px 10px;
      color: #f3f4f6;
      font-size: 13px;
      resize: vertical;
      font-family: inherit;
    }
    .attempt-textarea:focus {
      outline: none;
      border-color: #3b82f6;
    }
    .validation-message {
      font-size: 11px;
      padding: 6px 8px;
      border-radius: 4px;
      margin-top: 6px;
    }
    .error-msg {
      background: rgba(239, 68, 68, 0.15);
      color: #fca5a5;
      border: 1px solid rgba(239, 68, 68, 0.3);
    }
    .warning-msg {
      background: rgba(245, 158, 11, 0.15);
      color: #fcd34d;
      border: 1px solid rgba(245, 158, 11, 0.3);
    }
    .form-actions {
      display: flex;
      justify-content: flex-end;
      margin-top: 10px;
    }
    .submit-button {
      background: #2563eb;
      color: #ffffff;
      border: none;
      border-radius: 6px;
      padding: 8px 14px;
      font-size: 12px;
      font-weight: 600;
      cursor: pointer;
      transition: background 0.2s ease;
    }
    .submit-button:hover:not(:disabled) {
      background: #1d4ed8;
    }
    .submit-button:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }
  `]
})
export class AttemptSubmissionComponent {
  attemptContent = '';
  isSubmitting = false;

  readonly validationError = signal<string | null>(null);
  readonly isValid = signal(false);
  isWarning = false;

  readonly attemptSubmitted = output<SubmitAttemptRequest>();

  onContentChange(text: string): void {
    const trimmed = text.trim();
    this.isWarning = false;

    if (!trimmed) {
      this.validationError.set(null);
      this.isValid.set(false);
      return;
    }

    // Solution solicitation detection
    if (/((give|show|send|provide|write)\s+(me\s+)?(the\s+)?((full|complete|entire|whole)\s+)*(solution|code|answer))/i.test(trimmed)) {
      this.validationError.set("Codexo gives progressive hints, not full solutions! Describe what you've tried so far.");
      this.isWarning = true;
      this.isValid.set(false);
      return;
    }

    // Junk / repeated character pattern detection
    if (/^(.)\1{4,}$/.test(trimmed) || /^(asdf|qwerty|test|aaaa|1234|idk)+$/i.test(trimmed)) {
      this.validationError.set('Please provide a genuine problem-solving attempt.');
      this.isValid.set(false);
      return;
    }

    if (trimmed.length < 10) {
      this.validationError.set('Please write at least 10 characters describing your thinking.');
      this.isValid.set(false);
      return;
    }

    this.validationError.set(null);
    this.isValid.set(true);
  }

  onSubmit(): void {
    if (!this.isValid() || this.isSubmitting) return;

    this.attemptSubmitted.emit({
      attemptContent: this.attemptContent.trim(),
    });
  }

  resetForm(): void {
    this.attemptContent = '';
    this.validationError.set(null);
    this.isValid.set(false);
    this.isSubmitting = false;
  }
}
