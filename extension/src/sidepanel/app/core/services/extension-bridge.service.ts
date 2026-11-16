import { Injectable, signal } from '@angular/core';
import {
  MessageType,
  ExtensionMessage,
  ExtensionResponse,
  ProblemContextPayload,
} from '../../../../shared/extension-messaging';

@Injectable({
  providedIn: 'root',
})
export class ExtensionBridgeService {
  readonly isBridgeConnected = signal<boolean>(false);
  readonly activeProblem = signal<ProblemContextPayload | null>(null);
  readonly lastPingResponse = signal<string | null>(null);

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

    // 2. Fetch current active problem cached by the service worker
    chrome.runtime.sendMessage(
      { type: MessageType.GetActiveProblem },
      (response: ExtensionResponse<ProblemContextPayload | null>) => {
        if (!chrome.runtime.lastError && response?.success && response.data) {
          this.activeProblem.set(response.data);
          console.log('[ExtensionBridge] Loaded active problem from SW:', response.data.title);
        }
      }
    );

    // 3. Listen for broadcast problem updates from content script / service worker
    chrome.runtime.onMessage.addListener((message: ExtensionMessage) => {
      if (
        message.type === MessageType.ProblemDetected ||
        message.type === MessageType.ProblemUpdated
      ) {
        const payload = message.payload as ProblemContextPayload;
        if (payload) {
          this.activeProblem.set(payload);
          console.log('[ExtensionBridge] Received problem broadcast:', payload.title);
        }
      }
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
