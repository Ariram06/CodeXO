// Codexo Sidepanel API Contracts (Aligned with Codexo.Core / Codexo.Api DTOs)

export interface ProblemContextPayload {
  platform: number;
  platformProblemId: string;
  title: string;
  slug: string;
  url: string;
  difficulty: string;
  description: string;
}

export interface ProblemSessionResponse {
  sessionId: string;
  problemId: string;
  title: string;
  platform: number;
  platformProblemId: string;
  currentLevel: number;
  status: string;
  createdAt: string;
  lastActivityAt: string;
}

export interface HintResponseDto {
  hintId: string;
  level: number;
  levelName: string;
  content: string;
  observations?: string[];
  guidingQuestions?: string[];
  keyTakeaway?: string;
  createdAt: string;
  feedback?: {
    isHelpful: boolean;
    comment?: string;
  };
}

export interface HintStatusDto {
  currentLevel: number;
  currentLevelName: string;
  unlockedLevels: number[];
  totalHintsUnlocked: number;
  requiresAttemptForNextLevel: boolean;
  sessionStatus: string;
}

export interface SubmitAttemptRequest {
  attemptContent: string;
  codeSnippet?: string;
}

export interface SubmitAttemptResponse {
  attemptId: string;
  isApproved: boolean;
  feedbackMessage: string;
  nextLevelUnlocked: boolean;
  nextLevel: number;
}

export interface SubmitFeedbackRequest {
  isHelpful: boolean;
  comment?: string;
}

export interface HintFeedbackResponse {
  feedbackId: string;
  hintId: string;
  isHelpful: boolean;
  comment?: string;
  createdAt: string;
}

export interface UserPreferenceDto {
  preferredLanguage: string;
  autoUnlockL1: boolean;
  updatedAt: string;
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
  email: string;
}
