// Codexo Extension Messaging Types
// Shared between content script, service worker, and side panel

/**
 * All message types in the Codexo extension.
 * Each part of the extension sends/receives these typed messages.
 */
export enum MessageType {
  // Content script → Service worker
  ProblemDetected = "PROBLEM_DETECTED",
  ProblemUpdated = "PROBLEM_UPDATED",

  // Service worker → Side panel
  ProblemContext = "PROBLEM_CONTEXT",

  // Side panel → Service worker
  RequestHint = "REQUEST_HINT",

  // General
  Ping = "PING",
  Pong = "PONG",
}

export interface ExtensionMessage {
  type: MessageType;
  payload?: unknown;
}
