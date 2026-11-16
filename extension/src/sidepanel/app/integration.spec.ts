import { describe, it, expect, beforeEach, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import * as fs from 'node:fs';
import * as path from 'node:path';
import { App } from './app';
import { ExtensionBridgeService } from './core/services/extension-bridge.service';
import { ApiClientService } from './core/services/api-client.service';
import { extractProblemFromDocument } from '../../content/leetcode-extractor';
import { MessageType, ProblemContextPayload } from '../../shared/extension-messaging';

describe('End-to-End Integration Flow (DOM Scrape -> Messaging -> Sidepanel -> API)', () => {
  let fixture: ComponentFixture<App>;
  let app: App;
  let mockApiClient: any;

  beforeEach(async () => {
    (globalThis as any).chrome = {
      runtime: {
        sendMessage: vi.fn((_msg, cb) => cb?.({ success: true })),
        onMessage: { addListener: vi.fn() },
      },
      storage: {
        local: {
          get: vi.fn((_keys, cb) => cb?.({ codexo_token: 'valid-jwt-token', codexo_email: 'learner@codexo.dev' })),
          set: vi.fn(),
          remove: vi.fn(),
        },
      },
    };

    mockApiClient = {
      token: vi.fn(() => 'valid-jwt-token'),
      currentUser: vi.fn(() => 'learner@codexo.dev'),
      getOrCreateSession: vi.fn((problem: ProblemContextPayload) =>
        of({
          sessionId: 'session-e2e-1',
          problemId: 'prob-1',
          title: problem.title,
          platform: problem.platform,
          platformProblemId: problem.platformProblemId,
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
      requestNextHint: vi.fn(() =>
        of({
          hintId: 'hint-l1',
          level: 1,
          levelName: 'L1_Understand',
          content: 'Restated: find two indices summing to target.',
          observations: ['Array is unsorted', 'Only one valid answer exists'],
          guidingQuestions: ['Can we do better than O(n^2)?'],
          keyTakeaway: 'Look for complement in O(1).',
          createdAt: new Date().toISOString(),
        })
      ),
      submitAttempt: vi.fn(() =>
        of({
          attemptId: 'att-1',
          isApproved: true,
          feedbackMessage: 'Great approach!',
          nextLevelUnlocked: true,
          nextLevel: 2,
        })
      ),
      submitFeedback: vi.fn(() => of({ feedbackId: 'fb-1', isHelpful: true })),
      getPreferences: vi.fn(() => of({ preferredLanguage: 'C#', autoUnlockL1: true, updatedAt: '' })),
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
  });

  it('should successfully execute the full pipeline: DOM fixture scrape -> bridge sync -> session intake -> hint unlock', async () => {
    // 1. Scrape problem from recorded LeetCode Two Sum DOM fixture
    const fixturePath = path.resolve(__dirname, '../../content/fixtures/two-sum.html');
    const fixtureHtml = fs.readFileSync(fixturePath, 'utf-8');
    const doc = new DOMParser().parseFromString(fixtureHtml, 'text/html');
    const url = 'https://leetcode.com/problems/two-sum/description/';

    const extracted = extractProblemFromDocument(doc, url);
    expect(extracted).not.toBeNull();
    expect(extracted?.title).toBe('Two Sum');
    expect(extracted?.difficulty).toBe('Easy');

    // 2. Simulate message bridge broadcasting problem to sidepanel
    app.bridge.activeProblem.set(extracted);
    fixture.detectChanges();

    // 3. Verify sidepanel invoked session intake
    expect(mockApiClient.getOrCreateSession).toHaveBeenCalledWith(extracted);
    expect(app.activeSession()?.title).toBe('Two Sum');

    // 4. Request initial hint (L1)
    app.onRequestNextHint();
    fixture.detectChanges();

    expect(mockApiClient.requestNextHint).toHaveBeenCalledWith('session-e2e-1');
    expect(app.sessionHints().length).toBe(1);
    expect(app.sessionHints()[0].content).toContain('Restated: find two indices summing to target.');

    // 5. Verify UI renders the unlocked hint and problem banner
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.problem-title')?.textContent).toContain('Two Sum');
    expect(compiled.querySelector('.hint-text')?.textContent).toContain('Restated: find two indices');
  });

  it('should handle network failure gracefully with an error banner', () => {
    mockApiClient.getOrCreateSession.mockReturnValue(
      throwError(() => ({ error: { detail: 'Network connection refused. Is Codexo API running?' } }))
    );

    const mockProblem: ProblemContextPayload = {
      platform: 1,
      platformProblemId: '1',
      title: 'Two Sum',
      slug: 'two-sum',
      url: 'https://leetcode.com/problems/two-sum/',
      difficulty: 'Easy',
      description: '...',
    };

    app.initializeSessionForProblem(mockProblem);
    fixture.detectChanges();

    expect(app.globalError()).toContain('Network connection refused');
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.error-banner')?.textContent).toContain('Network connection refused');
  });

  it('should display rate limit warning when receiving HTTP 429 Too Many Requests', () => {
    app.activeSession.set({
      sessionId: 'sess-rate-limit',
      problemId: 'prob-1',
      title: 'Two Sum',
      platform: 1,
      platformProblemId: '1',
      currentLevel: 1,
      status: 'Active',
      createdAt: '',
      lastActivityAt: '',
    });

    mockApiClient.requestNextHint.mockReturnValue(
      throwError(() => ({ status: 429, error: { title: 'Too Many Requests' } }))
    );

    app.onRequestNextHint();
    fixture.detectChanges();

    expect(app.globalError()).toContain('Hint rate limit reached (6 hints/min)');
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.error-banner')?.textContent).toContain('Hint rate limit reached');
  });
});
