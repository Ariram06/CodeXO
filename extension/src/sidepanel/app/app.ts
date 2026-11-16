import { Component, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ExtensionBridgeService } from './core/services/extension-bridge.service';
import { ApiClientService } from './core/services/api-client.service';
import {
  HintResponseDto,
  HintStatusDto,
  ProblemSessionResponse,
  SubmitAttemptRequest,
  UserPreferenceDto,
} from './core/models/api-contracts';
import { ProblemContextBannerComponent } from './features/problem-context/problem-context-banner.component';
import { HintLadderComponent } from './features/hint-panel/hint-ladder.component';
import { AttemptSubmissionComponent } from './features/hint-panel/attempt-submission.component';
import { PreferencesModalComponent } from './features/settings/preferences-modal.component';
import { AuthPanelComponent } from './features/authentication/auth-panel.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    ProblemContextBannerComponent,
    HintLadderComponent,
    AttemptSubmissionComponent,
    PreferencesModalComponent,
    AuthPanelComponent,
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  readonly title = 'Codexo';
  readonly bridge = inject(ExtensionBridgeService);
  readonly api = inject(ApiClientService);

  readonly activeSession = signal<ProblemSessionResponse | null>(null);
  readonly sessionHints = signal<HintResponseDto[]>([]);
  readonly hintStatus = signal<HintStatusDto | null>(null);
  readonly userPreferences = signal<UserPreferenceDto | null>(null);

  readonly isPreferencesOpen = signal(false);
  readonly isLoading = signal(false);
  readonly authError = signal<string | null>(null);
  readonly globalError = signal<string | null>(null);

  constructor() {
    // React to active problem changes from Chrome extension bridge
    effect(() => {
      const problem = this.bridge.activeProblem();
      if (problem && this.api.token()) {
        this.initializeSessionForProblem(problem);
      }
    });

    // Load initial preferences if authenticated
    effect(() => {
      if (this.api.token()) {
        this.loadPreferences();
      }
    });
  }

  initializeSessionForProblem(problem = this.bridge.activeProblem()): void {
    if (!problem) return;

    this.isLoading.set(true);
    this.globalError.set(null);

    this.api.getOrCreateSession(problem).subscribe({
      next: (session) => {
        this.activeSession.set(session);
        this.loadSessionHints(session.sessionId);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.globalError.set(err?.error?.detail || 'Unable to connect to study session.');
      },
    });
  }

  loadSessionHints(sessionId: string): void {
    this.api.getSessionHints(sessionId).subscribe({
      next: (hints) => {
        this.sessionHints.set(hints);
        this.loadHintStatus(sessionId);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.globalError.set(err?.error?.detail || 'Failed to load hints.');
      },
    });
  }

  loadHintStatus(sessionId: string): void {
    this.api.getHintStatus(sessionId).subscribe({
      next: (status) => {
        this.hintStatus.set(status);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      },
    });
  }

  loadPreferences(): void {
    this.api.getPreferences().subscribe({
      next: (prefs) => {
        this.userPreferences.set(prefs);
      },
      error: () => {
        // Preferences may not exist yet or defaults applied
      },
    });
  }

  onRequestNextHint(): void {
    const session = this.activeSession();
    if (!session) return;

    this.isLoading.set(true);
    this.globalError.set(null);

    this.api.requestNextHint(session.sessionId).subscribe({
      next: (hint) => {
        const current = this.sessionHints();
        this.sessionHints.set([...current, hint]);
        this.loadHintStatus(session.sessionId);
      },
      error: (err) => {
        this.isLoading.set(false);
        if (err?.status === 429) {
          this.globalError.set('Hint rate limit reached (6 hints/min). Please wait a moment.');
        } else {
          this.globalError.set(err?.error?.detail || 'Failed to generate next hint.');
        }
      },
    });
  }

  onAttemptSubmitted(request: SubmitAttemptRequest): void {
    const session = this.activeSession();
    if (!session) return;

    this.isLoading.set(true);
    this.globalError.set(null);

    this.api.submitAttempt(session.sessionId, request).subscribe({
      next: (response) => {
        if (response.isApproved) {
          // Attempt approved! Automatically request the unlocked hint level
          this.onRequestNextHint();
        } else {
          this.isLoading.set(false);
          this.globalError.set(response.feedbackMessage || 'Attempt requires more detail.');
        }
      },
      error: (err) => {
        this.isLoading.set(false);
        this.globalError.set(err?.error?.detail || 'Failed to submit attempt.');
      },
    });
  }

  onFeedbackSubmitted(event: { hintId: string; isHelpful: boolean; comment?: string }): void {
    this.api
      .submitFeedback(event.hintId, {
        isHelpful: event.isHelpful,
        comment: event.comment,
      })
      .subscribe({
        next: () => {
          // Feedback recorded
        },
        error: (err) => {
          console.warn('Failed to submit feedback:', err);
        },
      });
  }

  onPreferencesSaved(event: { preferredLanguage: string; autoUnlockL1: boolean }): void {
    this.api.updatePreferences(event).subscribe({
      next: (updated) => {
        this.userPreferences.set(updated);
      },
      error: (err) => {
        console.warn('Failed to save preferences:', err);
      },
    });
  }

  onLogin(credentials: { email: string; password: string }): void {
    this.isLoading.set(true);
    this.authError.set(null);
    this.api.login(credentials.email, credentials.password).subscribe({
      next: () => {
        this.isLoading.set(false);
        if (this.bridge.activeProblem()) {
          this.initializeSessionForProblem();
        }
      },
      error: (err) => {
        this.isLoading.set(false);
        this.authError.set(err?.error?.detail || 'Invalid email or password.');
      },
    });
  }

  onRegister(credentials: { email: string; password: string }): void {
    this.isLoading.set(true);
    this.authError.set(null);
    this.api.register(credentials.email, credentials.password).subscribe({
      next: () => {
        this.isLoading.set(false);
        if (this.bridge.activeProblem()) {
          this.initializeSessionForProblem();
        }
      },
      error: (err) => {
        this.isLoading.set(false);
        this.authError.set(err?.error?.detail || 'Registration failed.');
      },
    });
  }

  onSignOut(): void {
    this.api.clearToken();
    this.activeSession.set(null);
    this.sessionHints.set([]);
    this.hintStatus.set(null);
  }

  canRequestDirectHint(): boolean {
    const status = this.hintStatus();
    if (!status) return true; // L1 understand
    return !status.requiresAttemptForNextLevel && status.currentLevel < 5;
  }

  requiresAttempt(): boolean {
    const status = this.hintStatus();
    return !!status?.requiresAttemptForNextLevel && status.currentLevel < 5;
  }
}
