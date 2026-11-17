// Codexo Sidepanel API Contracts (Aligned with Codexo.Core / Codexo.Api DTOs)

export interface PageContext {
  platform: string;
  url: string;
  title: string;
  problemText?: string;
  language?: string;
  code?: string;
  lineCount?: number;
  selection?: string;
  errorText?: string;
  isTruncated?: boolean;
}

export interface SendMessageRequest {
  conversationId?: string;
  message: string;
  context?: PageContext;
}

export interface ChatMessageResponse {
  conversationId: string;
  messageId: string;
  role: string;
  content: string;
  createdAtUtc: string;
  model: string;
  inputTokens: number;
  outputTokens: number;
}

export interface SubmitFeedbackRequest {
  isHelpful: boolean;
  comment?: string;
}

export interface MessageFeedbackResponse {
  feedbackId: string;
  messageId: string;
  isHelpful: boolean;
  comment?: string;
  createdAtUtc: string;
}

export interface UserPreferenceDto {
  preferredLanguage: string;
  autoUnlockL1: boolean;
  updatedAt: string;
  reasoningLevel?: 'low' | 'medium' | 'high' | string;
  model?: string;
}

export interface UpdateUserPreferenceRequest {
  preferredLanguage?: string;
  autoUnlockL1?: boolean;
  reasoningLevel?: 'low' | 'medium' | 'high' | string;
}

export interface ConversationSummaryDto {
  id: string;
  title: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  messageCount: number;
}

export interface ConversationDetailDto {
  id: string;
  title: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  messages: ChatMessageResponse[];
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
  email: string;
}

export interface ChatMessageItem {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  timestamp: Date;
  model?: string;
  isStreaming?: boolean;
}

export interface ChatStreamChunk {
  conversationId: string;
  messageId: string;
  delta?: string | null;
  isDone: boolean;
  model?: string | null;
  error?: string | null;
}

export interface StreamResult {
  conversationId: string;
  messageId: string;
  fullContent: string;
  model?: string;
}
