import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-auth-panel',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="auth-box">
      @if (currentUser()) {
        <div class="user-logged-in">
          <div class="user-info">
            <svg class="avatar-icon" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
              <path d="M19 21v-2a4 4 0 0 0-4-4H9a4 4 0 0 0-4 4v2"/>
              <circle cx="12" cy="7" r="4"/>
            </svg>
            <span class="user-email">{{ currentUser() }}</span>
          </div>
          <button
            type="button"
            class="sign-out-btn"
            (click)="signOutClicked.emit()"
            aria-label="Sign out of account"
          >
            Sign Out
          </button>
        </div>
      } @else {
        <div class="auth-tabs" role="tablist">
          <button
            type="button"
            role="tab"
            class="tab-btn"
            [class.active]="isLoginMode"
            [attr.aria-selected]="isLoginMode"
            (click)="isLoginMode = true"
          >
            Sign In
          </button>
          <button
            type="button"
            role="tab"
            class="tab-btn"
            [class.active]="!isLoginMode"
            [attr.aria-selected]="!isLoginMode"
            (click)="isLoginMode = false"
          >
            Register
          </button>
        </div>

        <form (ngSubmit)="onSubmit()" class="auth-form">
          <div class="field-row">
            <input
              type="email"
              class="auth-input"
              placeholder="Email address"
              [(ngModel)]="email"
              name="email"
              required
              aria-label="Email address"
            />
          </div>
          <div class="field-row">
            <input
              type="password"
              class="auth-input"
              placeholder="Password (min 8 chars)"
              [(ngModel)]="password"
              name="password"
              required
              minlength="8"
              aria-label="Password"
            />
          </div>

          @if (authError()) {
            <div class="auth-error" role="alert">{{ authError() }}</div>
          }

          <div class="submit-row">
            <button
              type="submit"
              class="auth-btn"
              [disabled]="isLoading() || !isValid()"
              [attr.aria-label]="isLoginMode ? 'Sign In' : 'Create Account'"
            >
              {{ isLoading() ? 'Please wait...' : (isLoginMode ? 'Sign In' : 'Create Account') }}
            </button>
          </div>
        </form>
      }
    </div>
  `,
  styles: [`
    .auth-box {
      background: var(--color-surface);
      border: 1px solid var(--color-border);
      border-radius: var(--radius-controls);
      padding: 10px 12px;
      margin-bottom: 12px;
    }
    .user-logged-in {
      display: flex;
      align-items: center;
      justify-content: space-between;
      min-height: 32px;
    }
    .user-info {
      display: flex;
      align-items: center;
      gap: 8px;
      font-size: var(--text-sm);
      color: var(--color-text);
      min-width: 0;
    }
    .avatar-icon {
      color: var(--color-muted);
      flex-shrink: 0;
    }
    .user-email {
      font-weight: 500;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }
    .sign-out-btn {
      background: var(--color-surface-2);
      border: 1px solid var(--color-border);
      border-radius: var(--radius-sm);
      min-height: 32px;
      padding: 0 10px;
      font-size: var(--text-xs);
      font-weight: 500;
      color: var(--color-muted);
      cursor: pointer;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      transition: background var(--transition-fast), color var(--transition-fast);

      &:hover {
        background: var(--color-border);
        color: var(--color-text);
      }
    }
    .auth-tabs {
      display: flex;
      gap: 6px;
      margin-bottom: 10px;
    }
    .tab-btn {
      flex: 1;
      background: transparent;
      border: 1px solid transparent;
      border-radius: var(--radius-sm);
      min-height: 32px;
      padding: 0 8px;
      font-size: var(--text-sm);
      font-weight: 500;
      color: var(--color-muted);
      cursor: pointer;
      transition: all var(--transition-fast);

      &:hover {
        color: var(--color-text);
      }

      &.active {
        background: var(--color-surface-2);
        border-color: var(--color-border);
        color: var(--color-text);
        font-weight: 600;
      }
    }
    .auth-form {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }
    .auth-input {
      width: 100%;
      height: 34px;
      box-sizing: border-box;
      background: var(--color-bg);
      border: 1px solid var(--color-border);
      border-radius: var(--radius-sm);
      padding: 0 10px;
      font-size: var(--text-sm);
      color: var(--color-text);
      font-family: inherit;
      transition: border-color var(--transition-fast);

      &::placeholder {
        color: var(--color-muted);
      }

      &:focus {
        outline: none;
        border-color: var(--color-accent);
      }
    }
    .auth-error {
      font-size: var(--text-xs);
      color: var(--color-error-text);
      background: var(--color-error-bg);
      border: 1px solid var(--color-error-border);
      padding: 6px 8px;
      border-radius: var(--radius-sm);
      line-height: 1.4;
    }
    .submit-row {
      display: flex;
      justify-content: flex-end;
      margin-top: 2px;
    }
    .auth-btn {
      background: var(--color-accent);
      color: var(--color-text-on-accent);
      border: none;
      border-radius: var(--radius-sm);
      min-height: 32px;
      padding: 0 14px;
      font-size: var(--text-sm);
      font-weight: 600;
      cursor: pointer;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      transition: background var(--transition-fast);

      &:hover:not(:disabled) {
        background: var(--color-accent-hover);
      }

      &:disabled {
        opacity: 0.45;
        cursor: not-allowed;
      }
    }
  `]
})
export class AuthPanelComponent {
  readonly currentUser = input<string | null>(null);
  readonly isLoading = input(false);
  readonly authError = input<string | null>(null);

  readonly loginSubmitted = output<{ email: string; password: string }>();
  readonly registerSubmitted = output<{ email: string; password: string }>();
  readonly signOutClicked = output<void>();

  isLoginMode = true;
  email = '';
  password = '';

  isValid(): boolean {
    return this.email.includes('@') && this.password.length >= 8;
  }

  onSubmit(): void {
    if (!this.isValid()) return;

    if (this.isLoginMode) {
      this.loginSubmitted.emit({ email: this.email, password: this.password });
    } else {
      this.registerSubmitted.emit({ email: this.email, password: this.password });
    }
  }
}
