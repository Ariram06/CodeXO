import { describe, it, expect, beforeEach, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ExtensionBridgeService } from './extension-bridge.service';
import { MessageType } from '../../../../shared/extension-messaging';

describe('ExtensionBridgeService', () => {
  let service: ExtensionBridgeService;

  beforeEach(() => {
    // Setup Chrome API mock
    (globalThis as any).chrome = {
      runtime: {
        sendMessage: vi.fn((message, callback) => {
          if (message.type === MessageType.Ping) {
            callback?.({ success: true, data: 'PONG_MOCK' });
          }
        }),
        onMessage: {
          addListener: vi.fn(),
        },
      },
      tabs: {
        query: vi.fn((_queryInfo, callback) => {
          callback?.([{ id: 101, url: 'https://leetcode.com/problems/two-sum/', title: 'Two Sum - LeetCode' }]);
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

  it('should refresh and return active tab context with platform, url, and title', async () => {
    const context = await service.refreshActiveTabContext();
    expect(context.platform).toBe('LeetCode');
    expect(context.url).toBe('https://leetcode.com/problems/two-sum/');
    expect(context.title).toBe('Two Sum - LeetCode');
    expect(service.activeTabContext()).toEqual(context);
  });
});
