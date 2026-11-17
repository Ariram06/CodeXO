import { Component, ElementRef, ViewChild, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ExtensionBridgeService } from './core/services/extension-bridge.service';
import { ApiClientService } from './core/services/api-client.service';
import {
  ChatMessageItem,
  ChatMessageResponse,
  PageContext,
  UserPreferenceDto,
} from './core/models/api-contracts';
import { PreferencesModalComponent } from './features/settings/preferences-modal.component';
import { AuthPanelComponent } from './features/authentication/auth-panel.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    PreferencesModalComponent,
    AuthPanelComponent,
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  readonly title = 'Codexo';
  readonly bridge = inject(ExtensionBridgeService);
  readonly api = inject(ApiClientService);

  @ViewChild('messageContainer') private messageContainer?: ElementRef<HTMLDivElement>;

  readonly messages = signal<ChatMessageItem[]>([]);
  readonly conversationId = signal<string | null>(null);
  readonly inputMessage = signal<string>('');
  readonly isLoading = signal<boolean>(false);
  readonly chatError = signal<{ message: string; retryAfter?: number } | null>(null);
  readonly lastSentMessage = signal<string | null>(null);

  readonly userPreferences = signal<UserPreferenceDto | null>(null);
  readonly isPreferencesOpen = signal<boolean>(false);
  readonly authError = signal<string | null>(null);

  constructor() {
    // Load preferences when authenticated
    effect(() => {
      if (this.api.token()) {
        this.loadPreferences();
        this.bridge.refreshActiveTabContext();
      }
    });
  }

  loadPreferences(): void {
    this.api.getPreferences().subscribe({
      next: (prefs) => this.userPreferences.set(prefs),
      error: () => {},
    });
  }

  onLogin(credentials: { email: string; password: string }): void {
    this.isLoading.set(true);
    this.authError.set(null);

    this.api.login(credentials.email, credentials.password).subscribe({
      next: () => {
        this.isLoading.set(false);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.authError.set(err?.error?.message || 'Login failed. Please verify credentials.');
      },
    });
  }

  onRegister(credentials: { email: string; password: string }): void {
    this.isLoading.set(true);
    this.authError.set(null);

    this.api.register(credentials.email, credentials.password).subscribe({
      next: () => {
        this.isLoading.set(false);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.authError.set(err?.error?.message || 'Registration failed.');
      },
    });
  }

  onSignOut(): void {
    this.api.clearToken();
    this.messages.set([]);
    this.conversationId.set(null);
    this.chatError.set(null);
  }

  onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.onSendMessage();
    }
  }

  async onSendMessage(): Promise<void> {
    const text = this.inputMessage().trim();
    if (!text || this.isLoading()) {
      return;
    }

    const userMessage: ChatMessageItem = {
      id: crypto.randomUUID(),
      role: 'user',
      content: text,
      timestamp: new Date(),
    };

    this.messages.update((prev) => [...prev, userMessage]);
    this.lastSentMessage.set(text);
    this.inputMessage.set('');
    this.isLoading.set(true);
    this.chatError.set(null);
    this.scrollToBottom();

    const rawContext: PageContext = await this.bridge.refreshActiveTabContext();
    const context: PageContext = {
      ...rawContext,
      code: this.bridge.includeCode() ? rawContext.code : undefined,
    };

    this.api.sendMessage({
      conversationId: this.conversationId() ?? undefined,
      message: text,
      context,
    }).subscribe({
      next: (response: ChatMessageResponse) => {
        this.conversationId.set(response.conversationId);
        const assistantMessage: ChatMessageItem = {
          id: response.messageId,
          role: 'assistant',
          content: response.content,
          timestamp: new Date(response.createdAtUtc),
          model: response.model,
        };
        this.messages.update((prev) => [...prev, assistantMessage]);
        this.isLoading.set(false);
        this.scrollToBottom();
      },
      error: (err) => {
        this.isLoading.set(false);
        let errorMsg = 'Failed to generate response.';
        let retryAfter: number | undefined;

        if (err.status === 429) {
          errorMsg = err.error?.message || 'Rate limit exceeded. Please wait a moment before sending another message.';
          retryAfter = err.error?.retryAfterSeconds;
        } else if (err.status === 503) {
          errorMsg = err.error?.message || 'AI service is temporarily unavailable. Please try again shortly.';
        } else if (err.status === 401) {
          errorMsg = 'Session expired. Please sign in again.';
          this.api.clearToken();
        } else if (err.error?.message) {
          errorMsg = err.error.message;
        } else if (err.message) {
          errorMsg = err.message;
        }

        this.chatError.set({ message: errorMsg, retryAfter });
        this.scrollToBottom();
      },
    });
  }

  onRetry(): void {
    const lastMsg = this.lastSentMessage();
    if (lastMsg) {
      this.inputMessage.set(lastMsg);
      this.chatError.set(null);
      this.onSendMessage();
    }
  }

  onPreferencesUpdated(updated: Partial<UserPreferenceDto>): void {
    this.api.updatePreferences(updated).subscribe({
      next: (res) => this.userPreferences.set(res),
      error: () => {},
    });
  }

  private scrollToBottom(): void {
    setTimeout(() => {
      if (this.messageContainer?.nativeElement) {
        this.messageContainer.nativeElement.scrollTop = this.messageContainer.nativeElement.scrollHeight;
      }
    }, 50);
  }
}
