import { Component, ElementRef, ViewChild, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SafeHtml } from '@angular/platform-browser';
import { ExtensionBridgeService } from './core/services/extension-bridge.service';
import { ApiClientService } from './core/services/api-client.service';
import { MarkdownRendererService } from './core/services/markdown-renderer.service';
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
  readonly markdownRenderer = inject(MarkdownRendererService);

  @ViewChild('messageContainer') private messageContainer?: ElementRef<HTMLDivElement>;
  @ViewChild('composerInput') private composerInput?: ElementRef<HTMLTextAreaElement>;

  readonly messages = signal<ChatMessageItem[]>([]);
  readonly conversationId = signal<string | null>(null);
  readonly inputMessage = signal<string>('');
  readonly isLoading = signal<boolean>(false);
  readonly isUserScrolledUp = signal<boolean>(false);
  readonly chatError = signal<{ message: string; retryAfter?: number } | null>(null);
  readonly lastSentMessage = signal<string | null>(null);

  readonly userPreferences = signal<UserPreferenceDto | null>(null);
  readonly isPreferencesOpen = signal<boolean>(false);
  readonly authError = signal<string | null>(null);

  private activeAbortController: AbortController | null = null;

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
    this.onStopGenerating();
    this.api.clearToken();
    this.messages.set([]);
    this.conversationId.set(null);
    this.chatError.set(null);
  }

  onStopGenerating(): void {
    if (this.activeAbortController) {
      this.activeAbortController.abort();
      this.activeAbortController = null;
    }
    this.isLoading.set(false);
    this.messages.update((prev) =>
      prev.map((m) => (m.isStreaming ? { ...m, isStreaming: false } : m))
    );
  }

  renderAssistantMessage(content: string): SafeHtml {
    return this.markdownRenderer.renderToSafeHtml(content);
  }

  onContainerScroll(event: Event): void {
    const element = event.target as HTMLElement;
    if (!element) return;
    const distanceFromBottom = element.scrollHeight - element.scrollTop - element.clientHeight;
    // Consider scrolled up if user is more than 50px away from the bottom
    this.isUserScrolledUp.set(distanceFromBottom > 50);
  }

  onContainerClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    const copyBtn = target.closest('.copy-code-btn') as HTMLButtonElement | null;
    if (copyBtn) {
      event.stopPropagation();
      const encoded = copyBtn.getAttribute('data-code');
      if (encoded) {
        const code = decodeURIComponent(encoded);
        navigator.clipboard.writeText(code).then(() => {
          const originalText = copyBtn.textContent || 'Copy';
          copyBtn.textContent = 'Copied!';
          copyBtn.classList.add('copied');
          setTimeout(() => {
            copyBtn.textContent = originalText;
            copyBtn.classList.remove('copied');
          }, 2000);
        }).catch((err) => {
          console.error('Failed to copy code to clipboard', err);
        });
      }
    }
  }

  onComposerInput(): void {
    const textarea = this.composerInput?.nativeElement;
    if (!textarea) return;

    // Reset height to recalculate scrollHeight cleanly
    textarea.style.height = 'auto';
    const lineHeight = 19; // px per line
    const maxLines = 5;
    const minHeight = 38;
    const maxHeight = lineHeight * maxLines + 16; // line height + padding
    const scrollHeight = textarea.scrollHeight;

    if (scrollHeight > maxHeight) {
      textarea.style.height = `${maxHeight}px`;
      textarea.style.overflowY = 'auto';
    } else {
      textarea.style.height = `${Math.max(scrollHeight, minHeight)}px`;
      textarea.style.overflowY = 'hidden';
    }
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
    this.onComposerInput();
    this.isLoading.set(true);
    this.chatError.set(null);
    this.isUserScrolledUp.set(false);
    this.scrollToBottom(true);

    const rawContext: PageContext = await this.bridge.refreshActiveTabContext();
    const context: PageContext = {
      ...rawContext,
      code: this.bridge.includeCode() ? rawContext.code : undefined,
    };

    const assistantMessageId = crypto.randomUUID();
    const assistantMessage: ChatMessageItem = {
      id: assistantMessageId,
      role: 'assistant',
      content: '',
      timestamp: new Date(),
      isStreaming: true,
    };
    this.messages.update((prev) => [...prev, assistantMessage]);

    this.activeAbortController = new AbortController();

    try {
      const result = await this.api.streamMessage(
        {
          conversationId: this.conversationId() ?? undefined,
          message: text,
          context,
        },
        (delta: string) => {
          this.messages.update((prev) =>
            prev.map((m) =>
              m.id === assistantMessageId
                ? { ...m, content: m.content + delta }
                : m
            )
          );
          this.scrollToBottom();
        },
        this.activeAbortController.signal
      );

      if (result.conversationId) {
        this.conversationId.set(result.conversationId);
      }

      this.messages.update((prev) =>
        prev.map((m) =>
          m.id === assistantMessageId
            ? {
                ...m,
                id: result.messageId || m.id,
                content: result.fullContent || m.content,
                model: result.model || m.model,
                isStreaming: false,
              }
            : m
        )
      );
    } catch (err: any) {
      if (this.activeAbortController?.signal.aborted) {
        this.messages.update((prev) =>
          prev.map((m) =>
            m.id === assistantMessageId ? { ...m, isStreaming: false } : m
          )
        );
        return;
      }

      // If error occurred and no content was generated at all, remove placeholder
      this.messages.update((prev) => {
        const found = prev.find((m) => m.id === assistantMessageId);
        if (found && !found.content) {
          return prev.filter((m) => m.id !== assistantMessageId);
        }
        return prev.map((m) =>
          m.id === assistantMessageId ? { ...m, isStreaming: false } : m
        );
      });

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
    } finally {
      this.activeAbortController = null;
      this.isLoading.set(false);
      this.scrollToBottom();
    }
  }

  onRetry(): void {
    const lastMsg = this.lastSentMessage();
    if (lastMsg) {
      this.inputMessage.set(lastMsg);
      this.chatError.set(null);
      this.isUserScrolledUp.set(false);
      this.onSendMessage();
    }
  }

  onPreferencesUpdated(updated: Partial<UserPreferenceDto>): void {
    this.api.updatePreferences(updated).subscribe({
      next: (res) => this.userPreferences.set(res),
      error: () => {},
    });
  }

  scrollToBottom(force = false): void {
    if (!force && this.isUserScrolledUp()) {
      return;
    }
    setTimeout(() => {
      if (this.messageContainer?.nativeElement) {
        this.messageContainer.nativeElement.scrollTop = this.messageContainer.nativeElement.scrollHeight;
      }
    }, 40);
  }
}
