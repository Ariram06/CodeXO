// Codexo Extension Messaging Contracts
// Shared between content script, service worker, and side panel

export enum MessageType {
  // Content script -> Service worker -> Side panel
  ProblemDetected = 'PROBLEM_DETECTED',
  ProblemUpdated = 'PROBLEM_UPDATED',
  PageContextUpdated = 'PAGE_CONTEXT_UPDATED',

  // Side panel -> Service worker / Content script
  GetActiveProblem = 'GET_ACTIVE_PROBLEM',
  RequestProblemExtraction = 'REQUEST_PROBLEM_EXTRACTION',
  GetPageContext = 'GET_PAGE_CONTEXT',
  RequestOptionalPermissions = 'REQUEST_OPTIONAL_PERMISSIONS',

  // Service worker -> MAIN world execution
  ExtractEditorCode = 'EXTRACT_EDITOR_CODE',

  // Diagnostic ping / pong
  Ping = 'PING',
  Pong = 'PONG',
}

export interface ProblemContextPayload {
  platform: number; // 1 = LeetCode
  platformProblemId: string;
  title: string;
  slug: string;
  url: string;
  difficulty: string;
  description: string;
}

export interface PageContextPayload {
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

export interface ExtensionMessage<T = unknown> {
  type: MessageType;
  payload?: T;
  timestamp?: number;
}

export interface ExtensionResponse<T = unknown> {
  success: boolean;
  data?: T;
  error?: string;
}
