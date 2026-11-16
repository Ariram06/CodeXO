// Codexo Extension Messaging Contracts
// Shared between content script, service worker, and side panel

export enum MessageType {
  // Content script -> Service worker -> Side panel
  ProblemDetected = "PROBLEM_DETECTED",
  ProblemUpdated = "PROBLEM_UPDATED",

  // Side panel -> Service worker / Content script
  GetActiveProblem = "GET_ACTIVE_PROBLEM",
  RequestProblemExtraction = "REQUEST_PROBLEM_EXTRACTION",

  // Diagnostic ping / pong
  Ping = "PING",
  Pong = "PONG",
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
