import { describe, it, expect, beforeEach, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { App } from './app';
import { ExtensionBridgeService } from './core/services/extension-bridge.service';
import { ApiClientService } from './core/services/api-client.service';
import { ChatMessageResponse } from './core/models/api-contracts';

describe('App Component', () => {
  let fixture: ComponentFixture<App>;
  let app: App;
  let mockApiClientService: any;

  beforeEach(async () => {
    (globalThis as any).chrome = {
      runtime: {
        sendMessage: vi.fn((_msg, cb) => cb?.({ success: true, data: 'PONG' })),
        onMessage: { addListener: vi.fn() },
      },
      tabs: {
        query: vi.fn((_q, cb) => cb?.([{ id: 1, url: 'https://leetcode.com/problems/two-sum/', title: 'Two Sum - LeetCode' }])),
      },
      storage: {
        local: {
          get: vi.fn((_keys, cb) => cb?.({ codexo_token: 'mock-jwt-token', codexo_email: 'dev@codexo.dev' })),
          set: vi.fn(),
          remove: vi.fn(),
        },
      },
    };

    mockApiClientService = {
      token: vi.fn(() => 'mock-jwt-token'),
      currentUser: vi.fn(() => 'dev@codexo.dev'),
      getPreferences: vi.fn(() =>
        of({
          preferredLanguage: 'Python',
          autoUnlockL1: false,
          updatedAt: new Date().toISOString(),
        })
      ),
      sendMessage: vi.fn(() =>
        of({
          conversationId: 'conv-123',
          messageId: 'msg-asst-1',
          role: 'assistant',
          content: 'Here is the O(n) hash map solution for Two Sum in Python.',
          createdAtUtc: new Date().toISOString(),
          model: 'gemini-3.5-flash-lite',
          inputTokens: 50,
          outputTokens: 120,
        } as ChatMessageResponse)
      ),
      updatePreferences: vi.fn(),
      clearToken: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        ExtensionBridgeService,
        { provide: ApiClientService, useValue: mockApiClientService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(App);
    app = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create the app and render header title', () => {
    expect(app).toBeTruthy();
    expect(app.title).toEqual('Codexo');
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.brand-name')?.textContent).toContain('Codexo');
  });

  it('should render empty state when no messages have been sent', () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.empty-title')?.textContent).toContain('Codexo AI Chat');
  });

  it('should send chat message and render user and assistant responses on Enter key', async () => {
    app.inputMessage.set('How do I optimize Two Sum?');
    const preventDefaultSpy = vi.fn();

    app.onKeyDown({
      key: 'Enter',
      shiftKey: false,
      preventDefault: preventDefaultSpy,
    } as unknown as KeyboardEvent);

    expect(preventDefaultSpy).toHaveBeenCalled();

    // Allow async state updates to settle
    await fixture.whenStable();
    fixture.detectChanges();

    expect(mockApiClientService.sendMessage).toHaveBeenCalled();

    expect(app.messages().length).toBe(2);
    expect(app.messages()[0].role).toBe('user');
    expect(app.messages()[0].content).toBe('How do I optimize Two Sum?');
    expect(app.messages()[1].role).toBe('assistant');
    expect(app.messages()[1].content).toContain('O(n) hash map solution');

    const compiled = fixture.nativeElement as HTMLElement;
    const messageRows = compiled.querySelectorAll('.message-row');
    expect(messageRows.length).toBe(2);
  });

  it('should allow multi-line input on Shift+Enter without sending', () => {
    app.inputMessage.set('def twoSum():');
    const preventDefaultSpy = vi.fn();

    app.onKeyDown({
      key: 'Enter',
      shiftKey: true,
      preventDefault: preventDefaultSpy,
    } as unknown as KeyboardEvent);

    expect(preventDefaultSpy).not.toHaveBeenCalled();
    expect(mockApiClientService.sendMessage).not.toHaveBeenCalled();
    expect(app.messages().length).toBe(0);
  });

  it('should display specific error banner with Retry button when chat API returns 429', async () => {
    mockApiClientService.sendMessage = vi.fn(() =>
      throwError(() => ({
        status: 429,
        error: { message: 'Rate limit exceeded.', retryAfterSeconds: 45 },
      }))
    );

    app.inputMessage.set('Trigger rate limit');
    await app.onSendMessage();
    fixture.detectChanges();

    expect(app.chatError()).not.toBeNull();
    expect(app.chatError()?.message).toContain('Rate limit exceeded');
    expect(app.chatError()?.retryAfter).toBe(45);

    const compiled = fixture.nativeElement as HTMLElement;
    const errorBanner = compiled.querySelector('.chat-error-banner');
    expect(errorBanner).not.toBeNull();
    expect(errorBanner?.textContent).toContain('Retry');
  });

  it('should resend last message when Retry button is clicked', async () => {
    mockApiClientService.sendMessage = vi.fn()
      .mockImplementationOnce(() => throwError(() => ({ status: 503, error: { message: 'Service unavailable' } })))
      .mockImplementationOnce(() =>
        of({
          conversationId: 'conv-retry-1',
          messageId: 'msg-2',
          role: 'assistant',
          content: 'Recovered answer on retry.',
          createdAtUtc: new Date().toISOString(),
          model: 'gemini-3.5-flash-lite',
          inputTokens: 10,
          outputTokens: 20,
        })
      );

    app.inputMessage.set('Explain hash map');
    await app.onSendMessage();
    fixture.detectChanges();

    expect(app.chatError()).not.toBeNull();

    // Click retry
    app.onRetry();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(mockApiClientService.sendMessage).toHaveBeenCalledTimes(2);
    expect(app.chatError()).toBeNull();
    expect(app.messages().some((m) => m.content.includes('Recovered answer on retry'))).toBe(true);
  });
});
