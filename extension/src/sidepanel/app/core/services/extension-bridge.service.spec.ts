import { describe, it, expect, beforeEach, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ExtensionBridgeService } from './extension-bridge.service';
import { MessageType } from '../../../../shared/extension-messaging';

describe('ExtensionBridgeService', () => {
  let service: ExtensionBridgeService;
  let messageListener: any;

  beforeEach(() => {
    messageListener = null;

    // Setup Chrome API mock
    (globalThis as any).chrome = {
      runtime: {
        sendMessage: vi.fn((message, callback) => {
          if (message.type === MessageType.Ping) {
            callback?.({ success: true, data: 'PONG_MOCK' });
          } else if (message.type === MessageType.GetPageContext) {
            callback?.({
              success: true,
              data: {
                platform: 'LeetCode',
                url: 'https://leetcode.com/problems/two-sum/',
                title: 'Two Sum - LeetCode',
                language: 'C++',
                code: 'class Solution { public: vector<int> twoSum() {} };',
                lineCount: 1,
              },
            });
          }
        }),
        onMessage: {
          addListener: vi.fn((listener) => {
            messageListener = listener;
          }),
        },
      },
      tabs: {
        query: vi.fn((_queryInfo, callback) => {
          callback?.([{ id: 101, url: 'https://leetcode.com/problems/two-sum/', title: 'Two Sum - LeetCode' }]);
        }),
      },
      permissions: {
        request: vi.fn((_opts, callback) => {
          callback?.(true);
        }),
      },
    };

    TestBed.configureTestingModule({
      providers: [ExtensionBridgeService],
    });

    service = TestBed.inject(ExtensionBridgeService);
  });

  it('should initialize and establish bridge connection upon successful ping', () => {
    expect(service.isBridgeConnected()).toBe(true);
    expect(service.lastPingResponse()).toBe('PONG_MOCK');
  });

  it('should refresh and return active tab context with platform, url, title, language, and code', async () => {
    const context = await service.refreshActiveTabContext();
    expect(context.platform).toBe('LeetCode');
    expect(context.url).toBe('https://leetcode.com/problems/two-sum/');
    expect(context.title).toBe('Two Sum - LeetCode');
    expect(context.language).toBe('C++');
    expect(context.code).toBe('class Solution { public: vector<int> twoSum() {} };');
    expect(service.activeTabContext()).toEqual(context);
  });

  it('should allow toggling includeCode state', () => {
    expect(service.includeCode()).toBe(true);
    service.setIncludeCode(false);
    expect(service.includeCode()).toBe(false);
    service.setIncludeCode(true);
    expect(service.includeCode()).toBe(true);
  });

  it('should update activeTabContext when PAGE_CONTEXT_UPDATED message is received', () => {
    expect(messageListener).toBeTruthy();
    messageListener({
      type: MessageType.PageContextUpdated,
      payload: {
        platform: 'Codeforces',
        url: 'https://codeforces.com/problemset/problem/1/A',
        title: 'Theatre Square',
        language: 'GNU C++20',
        lineCount: 15,
      },
    });

    const current = service.activeTabContext();
    expect(current?.platform).toBe('Codeforces');
    expect(current?.title).toBe('Theatre Square');
    expect(current?.lineCount).toBe(15);
  });

  it('should request optional permissions and return true when granted', async () => {
    const result = await service.requestOptionalHostPermission('https://example.com/*');
    expect(result).toBe(true);
    expect((chrome.permissions.request as any)).toHaveBeenCalledWith(
      { origins: ['https://example.com/*'] },
      expect.any(Function)
    );
  });
});
