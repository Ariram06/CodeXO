import { describe, it, expect, beforeEach, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ExtensionBridgeService } from './extension-bridge.service';
import { MessageType, ProblemContextPayload } from '../../../../shared/extension-messaging';

describe('ExtensionBridgeService', () => {
  let service: ExtensionBridgeService;

  beforeEach(() => {
    // Setup Chrome API mock
    (globalThis as any).chrome = {
      runtime: {
        sendMessage: vi.fn((message, callback) => {
          if (message.type === MessageType.Ping) {
            callback?.({ success: true, data: 'PONG_MOCK' });
          } else if (message.type === MessageType.GetActiveProblem) {
            callback?.({ success: true, data: null });
          }
        }),
        onMessage: {
          addListener: vi.fn(),
        },
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

  it('should update activeProblem signal when problem context is broadcast', () => {
    const mockProblem: ProblemContextPayload = {
      platform: 1,
      platformProblemId: '1',
      title: 'Two Sum',
      slug: 'two-sum',
      url: 'https://leetcode.com/problems/two-sum/',
      difficulty: 'Easy',
      description: 'Given nums and target...',
    };

    service.activeProblem.set(mockProblem);
    expect(service.activeProblem()).toEqual(mockProblem);
    expect(service.activeProblem()?.title).toBe('Two Sum');
  });
});
