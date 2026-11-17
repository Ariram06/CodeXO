import { Injectable, signal } from '@angular/core';
import {
  MessageType,
  ExtensionMessage,
  ExtensionResponse,
  PageContextPayload,
} from '../../../../shared/extension-messaging';
import { PageContext } from '../models/api-contracts';

@Injectable({
  providedIn: 'root',
})
export class ExtensionBridgeService {
  readonly isBridgeConnected = signal<boolean>(false);
  readonly lastPingResponse = signal<string | null>(null);
  readonly activeTabContext = signal<PageContext | null>(null);
  readonly includeCode = signal<boolean>(true);
  readonly isRefreshing = signal<boolean>(false);

  constructor() {
    this.initializeBridge();
  }

  initializeBridge(): void {
    if (typeof chrome === 'undefined' || !chrome.runtime?.sendMessage) {
      console.warn('[ExtensionBridge] Not running inside Chrome extension environment (standalone web mode).');
      return;
    }

    // 1. Diagnostic ping to verify service worker connectivity
    chrome.runtime.sendMessage(
      { type: MessageType.Ping, timestamp: Date.now() },
      (response: ExtensionResponse<string>) => {
        if (!chrome.runtime.lastError && response?.success) {
          this.isBridgeConnected.set(true);
          this.lastPingResponse.set(response.data ?? 'PONG_RECEIVED');
          console.log('[ExtensionBridge] Service worker bridge connected:', response.data);
        } else {
          console.warn('[ExtensionBridge] Ping failed or SW asleep:', chrome.runtime.lastError?.message);
        }
      }
    );

    // 2. Listen for push updates from service worker or content script (tab switches, SPA navigations)
    if (chrome.runtime.onMessage?.addListener) {
      chrome.runtime.onMessage.addListener((message: ExtensionMessage<PageContextPayload>) => {
        if (message.type === MessageType.PageContextUpdated && message.payload) {
          this.activeTabContext.set(message.payload);
        }
      });
    }

    // 3. Fetch initial active tab context
    this.refreshActiveTabContext();
  }

  setIncludeCode(include: boolean): void {
    this.includeCode.set(include);
  }

  async refreshActiveTabContext(): Promise<PageContext> {
    this.isRefreshing.set(true);

    if (typeof chrome === 'undefined' || !chrome.runtime?.sendMessage) {
      const mockContext: PageContext = {
        platform: 'LeetCode',
        url: 'https://leetcode.com/problems/two-sum/',
        title: 'Two Sum - LeetCode',
        language: 'Python3',
        code: 'def twoSum(nums, target):\n    lookup = {}\n    for i, n in enumerate(nums):\n        if target - n in lookup:\n            return [lookup[target - n], i]\n        lookup[n] = i',
        lineCount: 6,
      };
      this.activeTabContext.set(mockContext);
      this.isRefreshing.set(false);
      return mockContext;
    }

    return new Promise((resolve) => {
      // Ask service worker to capture full active tab context (MAIN-world editor + content script)
      chrome.runtime.sendMessage(
        { type: MessageType.GetPageContext },
        (response: ExtensionResponse<PageContextPayload>) => {
          this.isRefreshing.set(false);

          if (
            !chrome.runtime.lastError &&
            response?.success &&
            response.data &&
            typeof response.data === 'object' &&
            'platform' in response.data
          ) {
            this.activeTabContext.set(response.data);
            resolve(response.data);
            return;
          }

          // Fallback if service worker didn't return payload: direct tabs query
          if (chrome.tabs?.query) {
            chrome.tabs.query({ active: true, currentWindow: true }, (tabs) => {
              const activeTab = tabs?.[0];
              const url = activeTab?.url || '';
              const title = activeTab?.title || '';
              let platform = 'Web';
              if (url.includes('leetcode.com')) platform = 'LeetCode';
              else if (url.includes('codeforces.com')) platform = 'Codeforces';
              else if (url.includes('hackerrank.com')) platform = 'HackerRank';
              else if (url.includes('geeksforgeeks.org')) platform = 'GeeksforGeeks';
              else if (url.includes('codechef.com')) platform = 'CodeChef';

              const fallbackContext: PageContext = {
                platform,
                url,
                title,
              };
              this.activeTabContext.set(fallbackContext);
              resolve(fallbackContext);
            });
          } else {
            const emptyContext: PageContext = {
              platform: 'Web',
              url: '',
              title: 'Active Tab',
            };
            this.activeTabContext.set(emptyContext);
            resolve(emptyContext);
          }
        }
      );
    });
  }

  async requestOptionalHostPermission(origin: string): Promise<boolean> {
    if (typeof chrome === 'undefined' || !chrome.permissions?.request) {
      return true;
    }

    return new Promise((resolve) => {
      chrome.permissions.request({ origins: [origin] }, (granted) => {
        resolve(!!granted);
      });
    });
  }

  async ping(): Promise<string> {
    return new Promise((resolve, reject) => {
      if (typeof chrome === 'undefined' || !chrome.runtime?.sendMessage) {
        resolve('MOCK_PONG_DEV_MODE');
        return;
      }

      chrome.runtime.sendMessage({ type: MessageType.Ping }, (response: ExtensionResponse<string>) => {
        if (chrome.runtime.lastError) {
          reject(chrome.runtime.lastError);
        } else {
          this.isBridgeConnected.set(true);
          this.lastPingResponse.set(response.data ?? 'OK');
          resolve(response.data ?? 'PONG');
        }
      });
    });
  }
}
