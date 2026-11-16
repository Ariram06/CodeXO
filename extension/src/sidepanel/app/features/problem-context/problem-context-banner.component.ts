import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProblemContextPayload, ProblemSessionResponse } from '../../core/models/api-contracts';

@Component({
  selector: 'app-problem-context-banner',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (problem()) {
      <div class="problem-banner">
        <div class="banner-top">
          <span class="platform-badge">LeetCode #{{ problem()?.platformProblemId }}</span>
          <span class="difficulty-chip" [ngClass]="difficultyClass()">{{ problem()?.difficulty }}</span>
          @if (session()) {
            <span class="session-badge">Session: Active</span>
          }
        </div>
        <h2 class="problem-title">{{ problem()?.title }}</h2>
        <a [href]="problem()?.url" target="_blank" class="problem-link">
          Open on LeetCode &rarr;
        </a>
      </div>
    }
  `,
  styles: [`
    .problem-banner {
      background: #1e1e24;
      border: 1px solid #2e2e38;
      border-radius: 8px;
      padding: 12px 14px;
      margin-bottom: 12px;
    }
    .banner-top {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 6px;
    }
    .platform-badge {
      font-size: 11px;
      font-weight: 600;
      color: #9da3af;
      background: #2b2b36;
      padding: 2px 6px;
      border-radius: 4px;
    }
    .difficulty-chip {
      font-size: 11px;
      font-weight: 700;
      padding: 2px 6px;
      border-radius: 4px;
      text-transform: uppercase;
    }
    .diff-easy {
      color: #00b8a3;
      background: rgba(0, 184, 163, 0.15);
    }
    .diff-medium {
      color: #ffc01e;
      background: rgba(255, 192, 30, 0.15);
    }
    .diff-hard {
      color: #ff375f;
      background: rgba(255, 55, 95, 0.15);
    }
    .session-badge {
      font-size: 11px;
      color: #60a5fa;
      background: rgba(96, 165, 250, 0.15);
      padding: 2px 6px;
      border-radius: 4px;
      margin-left: auto;
    }
    .problem-title {
      font-size: 15px;
      font-weight: 600;
      color: #f3f4f6;
      margin: 0 0 6px 0;
    }
    .problem-link {
      font-size: 11px;
      color: #3b82f6;
      text-decoration: none;
    }
    .problem-link:hover {
      text-decoration: underline;
    }
  `]
})
export class ProblemContextBannerComponent {
  readonly problem = input<ProblemContextPayload | null>(null);
  readonly session = input<ProblemSessionResponse | null>(null);

  difficultyClass(): string {
    const diff = this.problem()?.difficulty?.toLowerCase();
    if (diff === 'easy') return 'diff-easy';
    if (diff === 'hard') return 'diff-hard';
    return 'diff-medium';
  }
}
