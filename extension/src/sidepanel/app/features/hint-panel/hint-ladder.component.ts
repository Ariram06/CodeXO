import { Component, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HintResponseDto, HintStatusDto } from '../../core/models/api-contracts';

interface LadderLevelDefinition {
  level: number;
  code: string;
  name: string;
  description: string;
}

@Component({
  selector: 'app-hint-ladder',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="hint-ladder">
      <div class="ladder-header">
        <h3 class="ladder-title">Pedagogical Hint Ladder</h3>
        <span class="progress-pill">
          {{ unlockedCount() }} of 5 Unlocked
        </span>
      </div>

      <div class="ladder-levels">
        @for (lvl of levels; track lvl.level) {
          @let hint = getHintForLevel(lvl.level);
          @let isUnlocked = !!hint;
          @let isCurrent = status()?.currentLevel === lvl.level;

          <div
            class="level-card"
            [class.unlocked]="isUnlocked"
            [class.locked]="!isUnlocked"
            [class.current]="isCurrent"
          >
            <div class="level-card-header" (click)="toggleExpand(lvl.level)">
              <div class="level-badge" [class.unlocked-badge]="isUnlocked">
                @if (isUnlocked) {
                  <span>✓</span>
                } @else {
                  <span>🔒</span>
                }
                <span class="level-code">{{ lvl.code }}</span>
              </div>
              <div class="level-title-group">
                <span class="level-name">{{ lvl.name }}</span>
                <span class="level-desc">{{ lvl.description }}</span>
              </div>
              @if (isUnlocked) {
                <span class="toggle-icon">{{ isExpanded(lvl.level) ? '▲' : '▼' }}</span>
              }
            </div>

            @if (isUnlocked && isExpanded(lvl.level) && hint) {
              <div class="level-content">
                <div class="hint-text">{{ hint.content }}</div>

                @if (hint.observations && hint.observations.length > 0) {
                  <div class="section-block">
                    <div class="section-title">Key Observations:</div>
                    <ul class="bullet-list">
                      @for (obs of hint.observations; track obs) {
                        <li>{{ obs }}</li>
                      }
                    </ul>
                  </div>
                }

                @if (hint.guidingQuestions && hint.guidingQuestions.length > 0) {
                  <div class="section-block">
                    <div class="section-title">Guiding Questions:</div>
                    <ul class="bullet-list questions">
                      @for (q of hint.guidingQuestions; track q) {
                        <li>{{ q }}</li>
                      }
                    </ul>
                  </div>
                }

                @if (hint.keyTakeaway) {
                  <div class="takeaway-box">
                    <strong>Takeaway:</strong> {{ hint.keyTakeaway }}
                  </div>
                }

                <!-- Feedback Row -->
                <div class="feedback-row">
                  <span class="feedback-label">Was this hint helpful?</span>
                  <div class="feedback-buttons">
                    <button
                      type="button"
                      class="thumb-btn"
                      [class.active]="hint.feedback?.isHelpful === true"
                      (click)="onThumbClick(hint, true)"
                      title="Helpful"
                    >
                      👍
                    </button>
                    <button
                      type="button"
                      class="thumb-btn"
                      [class.active]="hint.feedback?.isHelpful === false"
                      (click)="onThumbClick(hint, false)"
                      title="Needs improvement"
                    >
                      👎
                    </button>
                  </div>
                </div>

                @if (feedbackCommentHintId() === hint.hintId) {
                  <div class="comment-input-area">
                    <input
                      type="text"
                      class="comment-field"
                      placeholder="Optional: What could be clearer? (max 500 chars)"
                      [(ngModel)]="feedbackComment"
                      maxlength="500"
                    />
                    <button
                      type="button"
                      class="send-comment-btn"
                      (click)="submitFeedbackWithComment(hint)"
                    >
                      Send
                    </button>
                  </div>
                }
              </div>
            }
          </div>
        }
      </div>
    </div>
  `,
  styles: [`
    .hint-ladder {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }
    .ladder-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-bottom: 4px;
    }
    .ladder-title {
      font-size: 13px;
      font-weight: 600;
      color: #9da3af;
      margin: 0;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }
    .progress-pill {
      font-size: 11px;
      font-weight: 600;
      background: #1f2937;
      color: #3b82f6;
      padding: 2px 8px;
      border-radius: 12px;
    }
    .ladder-levels {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }
    .level-card {
      background: #141418;
      border: 1px solid #27272a;
      border-radius: 8px;
      overflow: hidden;
      transition: all 0.2s ease;
    }
    .level-card.unlocked {
      border-color: #3f3f46;
      background: #1a1a20;
    }
    .level-card.current {
      border-color: #3b82f6;
      box-shadow: 0 0 8px rgba(59, 130, 246, 0.2);
    }
    .level-card.locked {
      opacity: 0.6;
    }
    .level-card-header {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 10px 12px;
      cursor: pointer;
      user-select: none;
    }
    .level-badge {
      display: flex;
      align-items: center;
      gap: 4px;
      font-size: 11px;
      font-weight: 700;
      background: #27272a;
      color: #71717a;
      padding: 3px 6px;
      border-radius: 4px;
    }
    .unlocked-badge {
      background: rgba(16, 185, 129, 0.2);
      color: #10b981;
    }
    .level-title-group {
      display: flex;
      flex-direction: column;
      flex: 1;
    }
    .level-name {
      font-size: 13px;
      font-weight: 600;
      color: #f3f4f6;
    }
    .level-desc {
      font-size: 11px;
      color: #71717a;
    }
    .toggle-icon {
      font-size: 10px;
      color: #71717a;
    }
    .level-content {
      padding: 0 12px 12px 12px;
      border-top: 1px solid #27272a;
      font-size: 13px;
      color: #d1d5db;
      line-height: 1.5;
    }
    .hint-text {
      margin-top: 10px;
      white-space: pre-wrap;
    }
    .section-block {
      margin-top: 10px;
    }
    .section-title {
      font-size: 11px;
      font-weight: 700;
      color: #93c5fd;
      text-transform: uppercase;
      margin-bottom: 4px;
    }
    .bullet-list {
      margin: 0;
      padding-left: 18px;
      font-size: 12px;
      color: #e5e7eb;
    }
    .bullet-list.questions {
      color: #fef08a;
    }
    .bullet-list li {
      margin-bottom: 4px;
    }
    .takeaway-box {
      margin-top: 10px;
      padding: 8px 10px;
      background: rgba(59, 130, 246, 0.1);
      border-left: 3px solid #3b82f6;
      border-radius: 4px;
      font-size: 12px;
      color: #bfdbfe;
    }
    .feedback-row {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-top: 12px;
      padding-top: 8px;
      border-top: 1px dashed #27272a;
    }
    .feedback-label {
      font-size: 11px;
      color: #71717a;
    }
    .feedback-buttons {
      display: flex;
      gap: 6px;
    }
    .thumb-btn {
      background: #27272a;
      border: 1px solid #3f3f46;
      border-radius: 4px;
      padding: 4px 8px;
      font-size: 12px;
      cursor: pointer;
      transition: all 0.2s ease;
    }
    .thumb-btn:hover {
      background: #3f3f46;
    }
    .thumb-btn.active {
      background: #2563eb;
      border-color: #3b82f6;
    }
    .comment-input-area {
      display: flex;
      gap: 6px;
      margin-top: 8px;
    }
    .comment-field {
      flex: 1;
      background: #141418;
      border: 1px solid #3f3f46;
      border-radius: 4px;
      padding: 4px 8px;
      font-size: 11px;
      color: #f3f4f6;
    }
    .comment-field:focus {
      outline: none;
      border-color: #3b82f6;
    }
    .send-comment-btn {
      background: #3b82f6;
      color: #fff;
      border: none;
      border-radius: 4px;
      padding: 4px 10px;
      font-size: 11px;
      font-weight: 600;
      cursor: pointer;
    }
  `]
})
export class HintLadderComponent {
  readonly unlockedHints = input<HintResponseDto[]>([]);
  readonly status = input<HintStatusDto | null>(null);

  readonly feedbackSubmitted = output<{
    hintId: string;
    isHelpful: boolean;
    comment?: string;
  }>();

  readonly levels: LadderLevelDefinition[] = [
    { level: 1, code: 'L1', name: 'Understand', description: 'Restate problem, inputs/outputs, constraints' },
    { level: 2, code: 'L2', name: 'Direction', description: 'Pattern clues & guiding questions' },
    { level: 3, code: 'L3', name: 'Concepts', description: 'Data structures & algorithmic techniques' },
    { level: 4, code: 'L4', name: 'Steps', description: 'Plain English 3-6 step algorithm' },
    { level: 5, code: 'L5', name: 'Deep', description: 'Pitfalls, edge cases, complexity analysis' },
  ];

  private expandedLevels = signal<Record<number, boolean>>({ 1: true, 2: true, 3: true, 4: true, 5: true });
  readonly feedbackCommentHintId = signal<string | null>(null);
  feedbackComment = '';
  private currentVote: boolean | null = null;

  unlockedCount(): number {
    return this.unlockedHints()?.length || 0;
  }

  getHintForLevel(level: number): HintResponseDto | undefined {
    return this.unlockedHints()?.find((h) => h.level === level);
  }

  isExpanded(level: number): boolean {
    return !!this.expandedLevels()[level];
  }

  toggleExpand(level: number): void {
    const current = this.expandedLevels();
    this.expandedLevels.set({
      ...current,
      [level]: !current[level],
    });
  }

  onThumbClick(hint: HintResponseDto, isHelpful: boolean): void {
    this.currentVote = isHelpful;
    // Update local hint feedback state
    hint.feedback = {
      isHelpful,
      comment: hint.feedback?.comment,
    };

    // Open comment field
    this.feedbackCommentHintId.set(hint.hintId);

    // Emit feedback
    this.feedbackSubmitted.emit({
      hintId: hint.hintId,
      isHelpful,
      comment: hint.feedback?.comment,
    });
  }

  submitFeedbackWithComment(hint: HintResponseDto): void {
    if (this.currentVote === null) return;

    this.feedbackSubmitted.emit({
      hintId: hint.hintId,
      isHelpful: this.currentVote,
      comment: this.feedbackComment.trim() || undefined,
    });

    if (hint.feedback) {
      hint.feedback.comment = this.feedbackComment.trim();
    }

    this.feedbackCommentHintId.set(null);
    this.feedbackComment = '';
  }
}
