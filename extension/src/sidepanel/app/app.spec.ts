import { describe, it, expect, beforeEach, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { App } from './app';
import { ExtensionBridgeService } from './core/services/extension-bridge.service';
import { ApiClientService } from './core/services/api-client.service';
import { ChatMessageResponse } from './core/models/api-contracts';
import { MessageType } from '../../shared/extension-messaging';

describe('App Component', () => {
  let fixture: ComponentFixture<App>;
  let app: App;
  let mockApiClientService: any;

  beforeEach(async () => {
    (globalThis as any).chrome = {
      runtime: {
        sendMessage: vi.fn((msg, cb) => {
          if (msg?.type === 'PING' || msg?.type === MessageType.Ping) {
            cb?.({ success: true, data: 'PONG' });
          } else if (msg?.type === 'GET_PAGE_CONTEXT' || msg?.type === MessageType.GetPageContext) {
            cb?.({
              success: true,
              data: {
                platform: 'LeetCode',
                url: 'https://leetcode.com/problems/two-sum/',
                title: 'Two Sum - LeetCode',
                language: 'Python3',
                code: 'class Solution: pass',
                lineCount: 1,
              },
            });
          }
        }),
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
      streamMessage: vi.fn(async (request, onChunk, _signal) => {
        onChunk('Here is the O(n) hash map solution');
        onChunk(' for Two Sum in Python.');
        return {
          conversationId: 'conv-123',
          messageId: 'msg-asst-1',
          fullContent: 'Here is the O(n) hash map solution for Two Sum in Python.',
          model: 'gemini-3.5-flash-lite',
        };
      }),
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

    expect(mockApiClientService.streamMessage).toHaveBeenCalled();

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
    expect(mockApiClientService.streamMessage).not.toHaveBeenCalled();
    expect(app.messages().length).toBe(0);
  });

  it('should display specific error banner with Retry button when chat API returns 429', async () => {
    mockApiClientService.streamMessage = vi.fn().mockRejectedValue({
      status: 429,
      error: { message: 'Rate limit exceeded.', retryAfterSeconds: 45 },
    });

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
    mockApiClientService.streamMessage = vi.fn()
      .mockRejectedValueOnce({ status: 503, error: { message: 'Service unavailable' } })
      .mockResolvedValueOnce({
        conversationId: 'conv-retry-1',
        messageId: 'msg-2',
        fullContent: 'Recovered answer on retry.',
        model: 'gemini-3.5-flash-lite',
      });

    app.inputMessage.set('Explain hash map');
    await app.onSendMessage();
    fixture.detectChanges();

    expect(app.chatError()).not.toBeNull();

    // Click retry
    app.onRetry();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(mockApiClientService.streamMessage).toHaveBeenCalledTimes(2);
    expect(app.chatError()).toBeNull();
    expect(app.messages().some((m) => m.content.includes('Recovered answer on retry'))).toBe(true);
  });

  it('should incrementally render streamed chunks in the UI', async () => {
    let capturedOnChunk: (d: string) => void = () => {};
    mockApiClientService.streamMessage = vi.fn(async (_req, onChunk) => {
      capturedOnChunk = onChunk;
      // Simulate chunk 1
      capturedOnChunk('Chunk 1 ');
      return new Promise((resolve) => {
        setTimeout(() => {
          capturedOnChunk('Chunk 2');
          resolve({
            conversationId: 'conv-chunks',
            messageId: 'msg-chunks',
            fullContent: 'Chunk 1 Chunk 2',
            model: 'gemini-3.5-flash',
          });
        }, 10);
      });
    });

    app.inputMessage.set('Stream me');
    const sendPromise = app.onSendMessage();

    // Allow async context refresh to complete and trigger first chunk
    await new Promise((resolve) => setTimeout(resolve, 0));
    fixture.detectChanges();

    // After chunk 1
    expect(app.messages().length).toBe(2);
    expect(app.messages()[1].content).toBe('Chunk 1 ');
    expect(app.messages()[1].isStreaming).toBe(true);

    await sendPromise;
    expect(app.messages()[1].content).toBe('Chunk 1 Chunk 2');
    expect(app.messages()[1].isStreaming).toBe(false);
  });

  it('should stop generating and retain partial text when onStopGenerating is invoked', async () => {
    mockApiClientService.streamMessage = vi.fn(async (_req, onChunk, signal: AbortSignal) => {
      onChunk('Partial solution start...');
      return new Promise((_resolve, reject) => {
        signal.addEventListener('abort', () => {
          const abortError = new Error('The operation was aborted');
          abortError.name = 'AbortError';
          reject(abortError);
        });
      });
    });

    app.inputMessage.set('Give me a long answer');
    const sendPromise = app.onSendMessage();

    // Allow stream to start and yield partial text
    await new Promise((resolve) => setTimeout(resolve, 0));
    fixture.detectChanges();

    expect(app.isLoading()).toBe(true);
    expect(app.messages()[1].content).toBe('Partial solution start...');

    // User clicks Stop
    app.onStopGenerating();

    await sendPromise;
    expect(app.isLoading()).toBe(false);
    expect(app.messages()[1].content).toBe('Partial solution start...');
    expect(app.messages()[1].isStreaming).toBe(false);
  });

  it('should render context bar chips and respect includeCode toggle in streamMessage', async () => {
    // Set active tab context with code
    app.bridge.activeTabContext.set({
      platform: 'LeetCode',
      url: 'https://leetcode.com/problems/two-sum/',
      title: '1. Two Sum',
      language: 'Python3',
      code: 'class Solution: pass',
      lineCount: 1,
    });
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.platform-chip')?.textContent).toContain('LeetCode');
    expect(compiled.querySelector('.title-chip')?.textContent).toContain('1. Two Sum');
    expect(compiled.querySelector('.lang-chip')?.textContent).toContain('Python3');
    expect(compiled.querySelector('.code-chip')?.textContent).toContain('Code: 1 line');

    // 1. Send with includeCode = true (default)
    app.inputMessage.set('Check this');
    await app.onSendMessage();
    expect(mockApiClientService.streamMessage).toHaveBeenCalledWith(
      expect.objectContaining({
        context: expect.objectContaining({
          code: 'class Solution: pass',
        }),
      }),
      expect.any(Function),
      expect.anything()
    );

    // 2. Toggle includeCode = false
    app.bridge.setIncludeCode(false);
    fixture.detectChanges();
    expect(compiled.querySelector('.code-chip.excluded')?.textContent).toContain('Code: Excluded');

    app.inputMessage.set('Check this without code');
    await app.onSendMessage();
    expect(mockApiClientService.streamMessage).toHaveBeenCalledWith(
      expect.objectContaining({
        context: expect.objectContaining({
          code: undefined,
        }),
      }),
      expect.any(Function),
      expect.anything()
    );
  });

  it('should render assistant markdown and handle Copy button clicks via clipboard API', async () => {
    const clipboardSpy = vi.fn().mockResolvedValue(undefined);
    Object.assign(navigator, {
      clipboard: {
        writeText: clipboardSpy,
      },
    });

    app.messages.set([
      {
        id: 'user-1',
        role: 'user',
        content: 'Show me python code',
        timestamp: new Date(),
      },
      {
        id: 'asst-1',
        role: 'assistant',
        content: 'Here is the code:\n```python\ndef solve():\n    return 42\n```',
        timestamp: new Date(),
        model: 'gemini-3.5-flash-lite',
      },
    ]);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const markdownBody = compiled.querySelector('.markdown-body');
    expect(markdownBody).not.toBeNull();

    const copyBtn = compiled.querySelector('.copy-code-btn') as HTMLButtonElement | null;
    expect(copyBtn).not.toBeNull();
    expect(copyBtn?.textContent).toBe('Copy');

    // Click the Copy button
    copyBtn?.click();
    await fixture.whenStable();

    expect(clipboardSpy).toHaveBeenCalledWith('def solve():\n    return 42');
    expect(copyBtn?.textContent).toBe('Copied!');
  });

  it('should toggle isUserScrolledUp when scroll position changes', () => {
    expect(app.isUserScrolledUp()).toBe(false);

    // Simulate user scrolling up (distance from bottom = 600 - 300 - 200 = 100 > 50)
    app.onContainerScroll({
      target: {
        scrollHeight: 600,
        scrollTop: 300,
        clientHeight: 200,
      },
    } as unknown as Event);

    expect(app.isUserScrolledUp()).toBe(true);

    // Simulate user scrolling back to bottom (distance from bottom = 600 - 380 - 200 = 20 <= 50)
    app.onContainerScroll({
      target: {
        scrollHeight: 600,
        scrollTop: 380,
        clientHeight: 200,
      },
    } as unknown as Event);

    expect(app.isUserScrolledUp()).toBe(false);
  });

  it('should adjust composer textarea height on input', () => {
    fixture.detectChanges();
    const textarea = fixture.nativeElement.querySelector('.composer-input') as HTMLTextAreaElement;
    expect(textarea).not.toBeNull();

    // Trigger composer input
    app.inputMessage.set('Line 1\nLine 2\nLine 3');
    app.onComposerInput();

    expect(textarea.style.height).toBeTruthy();
  });
});
