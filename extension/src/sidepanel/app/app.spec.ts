import { describe, it, expect, beforeEach, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { App } from './app';
import { ExtensionBridgeService } from './core/services/extension-bridge.service';
import { ApiClientService } from './core/services/api-client.service';
import { ProblemContextPayload } from './core/models/api-contracts';

describe('App Component', () => {
  let fixture: ComponentFixture<App>;
  let app: App;
  let mockBridgeService: Partial<ExtensionBridgeService>;
  let mockApiClientService: any;

  beforeEach(async () => {
    (globalThis as any).chrome = {
      runtime: {
        sendMessage: vi.fn((_msg, cb) => cb?.({ success: true })),
        onMessage: { addListener: vi.fn() },
      },
      storage: {
        local: {
          get: vi.fn((_keys, cb) => cb?.({ codexo_token: 'mock-jwt-token', codexo_email: 'learner@codexo.dev' })),
          set: vi.fn(),
          remove: vi.fn(),
        },
      },
    };

    mockApiClientService = {
      token: vi.fn(() => 'mock-jwt-token'),
      currentUser: vi.fn(() => 'learner@codexo.dev'),
      getOrCreateSession: vi.fn(() =>
        of({
          sessionId: 'sess-123',
          problemId: 'prob-1',
          title: 'Two Sum',
          platform: 1,
          platformProblemId: '1',
          currentLevel: 1,
          status: 'Active',
          createdAt: new Date().toISOString(),
          lastActivityAt: new Date().toISOString(),
        })
      ),
      getSessionHints: vi.fn(() => of([])),
      getHintStatus: vi.fn(() =>
        of({
          currentLevel: 1,
          currentLevelName: 'L1_Understand',
          unlockedLevels: [1],
          totalHintsUnlocked: 1,
          requiresAttemptForNextLevel: false,
          sessionStatus: 'Active',
        })
      ),
      getPreferences: vi.fn(() =>
        of({
          preferredLanguage: 'C#',
          autoUnlockL1: true,
          updatedAt: new Date().toISOString(),
        })
      ),
      requestNextHint: vi.fn(),
      submitAttempt: vi.fn(),
      submitFeedback: vi.fn(),
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
  });

  it('should create the app and render header title', () => {
    expect(app).toBeTruthy();
    expect(app.title).toEqual('Codexo');
  });

  it('should render empty state when no problem is detected', () => {
    app.bridge.activeProblem.set(null);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.empty-title')?.textContent).toContain('No Problem Detected');
  });

  it('should render problem context banner when active problem is broadcast', () => {
    const mockProblem: ProblemContextPayload = {
      platform: 1,
      platformProblemId: '1',
      title: 'Two Sum',
      slug: 'two-sum',
      url: 'https://leetcode.com/problems/two-sum/',
      difficulty: 'Easy',
      description: 'Given nums and target...',
    };

    app.bridge.activeProblem.set(mockProblem);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.problem-title')?.textContent).toContain('Two Sum');
    expect(compiled.querySelector('.platform-badge')?.textContent).toContain('LeetCode #1');
  });
});
