import { Component, input, output, signal } from '@angular/core';
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
            <span class="avatar-icon">👤</span>
            <span class="user-email">{{ currentUser() }}</span>
          </div>
          <button type="button" class="sign-out-btn" (click)="signOutClicked.emit()">Sign Out</button>
        </div>
      } @else {
        <div class="auth-tabs">
          <button
            type="button"
            class="tab-btn"
            [class.active]="isLoginMode"
            (click)="isLoginMode = true"
          >
            Sign In
          </button>
          <button
            type="button"
            class="tab-btn"
            [class.active]="!isLoginMode"
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
            />
          </div>

          @if (authError()) {
            <div class="auth-error">{{ authError() }}</div>
          }

          <div class="submit-row">
            <button type="submit" class="auth-btn" [disabled]="isLoading() || !isValid()">
              {{ isLoading() ? 'Please wait...' : (isLoginMode ? 'Sign In' : 'Create Account') }}
            </button>
          </div>
        </form>
      }
    </div>
  `,
  styles: [`
    .auth-box {
      background: #18181b;
      border: 1px solid #27272a;
      border-radius: 8px;
      padding: 10px 12px;
      margin-bottom: 12px;
    }
    .user-logged-in {
      display: flex;
      align-items: center;
      justify-content: space-between;
    }
    .user-info {
      display: flex;
      align-items: center;
      gap: 6px;
      font-size: 12px;
      color: #e5e7eb;
    }
    .avatar-icon {
      font-size: 14px;
    }
    .user-email {
      font-weight: 500;
    }
    .sign-out-btn {
      background: #27272a;
      border: 1px solid #3f3f46;
      border-radius: 4px;
      padding: 3px 8px;
      font-size: 11px;
      color: #9ca3af;
      cursor: pointer;
    }
    .sign-out-btn:hover {
      background: #3f3f46;
      color: #f3f4f6;
    }
    .auth-tabs {
      display: flex;
      gap: 4px;
      margin-bottom: 8px;
    }
    .tab-btn {
      flex: 1;
      background: #27272a;
      border: none;
      border-radius: 4px;
      padding: 4px;
      font-size: 11px;
      font-weight: 600;
      color: #9ca3af;
      cursor: pointer;
    }
    .tab-btn.active {
      background: #3b82f6;
      color: #ffffff;
    }
    .auth-form {
      display: flex;
      flex-direction: column;
      gap: 6px;
    }
    .auth-input {
      width: 100%;
      box-sizing: border-box;
      background: #141418;
      border: 1px solid #27272a;
      border-radius: 4px;
      padding: 6px 8px;
      font-size: 12px;
      color: #f3f4f6;
    }
    .auth-input:focus {
      outline: none;
      border-color: #3b82f6;
    }
    .auth-error {
      font-size: 11px;
      color: #fca5a5;
      background: rgba(239, 68, 68, 0.15);
      padding: 4px 6px;
      border-radius: 4px;
    }
    .submit-row {
      display: flex;
      justify-content: flex-end;
    }
    .auth-btn {
      background: #2563eb;
      color: #ffffff;
      border: none;
      border-radius: 4px;
      padding: 5px 12px;
      font-size: 11px;
      font-weight: 600;
      cursor: pointer;
    }
    .auth-btn:disabled {
      opacity: 0.5;
      cursor: not-allowed;
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
