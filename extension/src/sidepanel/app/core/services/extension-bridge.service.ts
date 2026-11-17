import { Injectable, signal } from '@angular/core';
import {
  MessageType,
  ExtensionMessage,
  ExtensionResponse,
} from '../../../../shared/extension-messaging';
import { PageContext } from '../models/api-contracts';

@Injectable({
  providedIn: 'root',
})
export class ExtensionBridgeService {
  readonly isBridgeConnected = signal<boolean>(false);
  readonly lastPingResponse = signal<string | null>(null);
  readonly activeTabContext = signal<PageContext | null>(null);

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

    // 2. Fetch initial active tab context
    this.refreshActiveTabContext();
  }

  async refreshActiveTabContext(): Promise<PageContext> {
    if (typeof chrome === 'undefined' || !chrome.tabs?.query) {
      const mockContext: PageContext = {
        platform: 'Web',
        url: 'https://leetcode.com/problems/two-sum/',
        title: 'Two Sum - LeetCode',
      };
      this.activeTabContext.set(mockContext);
      return mockContext;
    }

    return new Promise((resolve) => {
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

        const context: PageContext = {
          platform,
          url,
          title,
        };
        this.activeTabContext.set(context);
        resolve(context);
      });
    });
  }

  // Method to manually ping or test messaging
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
