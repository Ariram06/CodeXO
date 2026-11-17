import { describe, it, expect, beforeEach, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { App } from './app';
import { ExtensionBridgeService } from './core/services/extension-bridge.service';
import { ApiClientService } from './core/services/api-client.service';
import { ChatMessageResponse, SendMessageRequest } from './core/models/api-contracts';

describe('End-to-End Chat Integration Flow (Tab Context -> Bridge -> Sidepanel -> API)', () => {
  let fixture: ComponentFixture<App>;
  let app: App;
  let mockApiClient: any;

  beforeEach(async () => {
    (globalThis as any).chrome = {
      runtime: {
        sendMessage: vi.fn((_msg, cb) => cb?.({ success: true, data: 'PONG' })),
        onMessage: { addListener: vi.fn() },
      },
      tabs: {
        query: vi.fn((_q, cb) =>
          cb?.([{ id: 101, url: 'https://leetcode.com/problems/two-sum/', title: 'Two Sum - LeetCode' }])
        ),
      },
      storage: {
        local: {
          get: vi.fn((_keys, cb) => cb?.({ codexo_token: 'valid-jwt-token', codexo_email: 'dev@codexo.dev' })),
          set: vi.fn(),
          remove: vi.fn(),
        },
      },
    };

    mockApiClient = {
      token: vi.fn(() => 'valid-jwt-token'),
      currentUser: vi.fn(() => 'dev@codexo.dev'),
      getPreferences: vi.fn(() => of({ preferredLanguage: 'Python', autoUnlockL1: false, updatedAt: '' })),
      sendMessage: vi.fn((req: SendMessageRequest) =>
        of({
          conversationId: 'conv-e2e-1',
          messageId: 'msg-asst-101',
          role: 'assistant',
          content: 'The optimal Two Sum solution uses a hash map to achieve O(n) time and O(n) space complexity.',
          createdAtUtc: new Date().toISOString(),
          model: 'gemini-3.5-flash-lite',
          inputTokens: 42,
          outputTokens: 88,
        } as ChatMessageResponse)
      ),
      updatePreferences: vi.fn(),
      clearToken: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        ExtensionBridgeService,
        { provide: ApiClientService, useValue: mockApiClient },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(App);
    app = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should execute full chat flow: active tab context -> composer -> API call -> assistant response rendering', async () => {
    // 1. Verify active tab context was loaded from bridge
    await app.bridge.refreshActiveTabContext();
    expect(app.bridge.activeTabContext()?.platform).toBe('LeetCode');
    expect(app.bridge.activeTabContext()?.title).toBe('Two Sum - LeetCode');

    // 2. User enters query and sends
    app.inputMessage.set('What is the time complexity of the optimal Two Sum solution?');
    await app.onSendMessage();
    await fixture.whenStable();
    fixture.detectChanges();

    // 3. Verify API client received message and active page context
    expect(mockApiClient.sendMessage).toHaveBeenCalledWith(
      expect.objectContaining({
        message: 'What is the time complexity of the optimal Two Sum solution?',
        context: expect.objectContaining({
          platform: 'LeetCode',
          url: 'https://leetcode.com/problems/two-sum/',
          title: 'Two Sum - LeetCode',
        }),
      })
    );

    // 4. Verify messages rendered in the DOM
    const compiled = fixture.nativeElement as HTMLElement;
    const bubbles = compiled.querySelectorAll('.message-bubble');
    expect(bubbles.length).toBe(2);

    // User bubble
    expect(bubbles[0].textContent).toContain('What is the time complexity');

    // Assistant bubble
    expect(bubbles[1].textContent).toContain('O(n) time and O(n) space complexity');
    expect(bubbles[1].textContent).toContain('gemini-3.5-flash-lite');
  });

  it('should handle API rate limit 429 with specific error banner and retry capability', async () => {
    mockApiClient.sendMessage = vi.fn()
      .mockImplementationOnce(() =>
        throwError(() => ({
          status: 429,
          error: { message: 'Too many requests. Please wait a moment.', retryAfterSeconds: 60 },
        }))
      )
      .mockImplementationOnce(() =>
        of({
          conversationId: 'conv-e2e-retry',
          messageId: 'msg-asst-102',
          role: 'assistant',
          content: 'Here is the requested explanation after waiting for backoff.',
          createdAtUtc: new Date().toISOString(),
          model: 'gemini-3.5-flash-lite',
          inputTokens: 20,
          outputTokens: 40,
        })
      );

    app.inputMessage.set('Explain the two-pointer approach');
    await app.onSendMessage();
    await fixture.whenStable();
    fixture.detectChanges();

    // Verify error banner is visible
    const compiled = fixture.nativeElement as HTMLElement;
    const banner = compiled.querySelector('.chat-error-banner');
    expect(banner).not.toBeNull();
    expect(banner?.textContent).toContain('Too many requests');
    expect(banner?.textContent).toContain('60s');

    // User clicks Retry
    app.onRetry();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(mockApiClient.sendMessage).toHaveBeenCalledTimes(2);
    expect(app.chatError()).toBeNull();
    expect(app.messages().some((m) => m.content.includes('Here is the requested explanation'))).toBe(true);
  });
});
