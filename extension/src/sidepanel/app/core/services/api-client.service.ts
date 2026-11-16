import { Injectable, signal } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import {
  AuthResponse,
  HintFeedbackResponse,
  HintResponseDto,
  HintStatusDto,
  ProblemContextPayload,
  ProblemSessionResponse,
  SubmitAttemptRequest,
  SubmitAttemptResponse,
  SubmitFeedbackRequest,
  UserPreferenceDto,
} from '../models/api-contracts';

@Injectable({
  providedIn: 'root',
})
export class ApiClientService {
  private baseUrl = 'http://localhost:5275';
  readonly token = signal<string | null>(null);
  readonly currentUser = signal<string | null>(null);

  constructor(private http: HttpClient) {
    this.restoreToken();
    this.restoreBaseUrl();
  }

  getBaseUrl(): string {
    return this.baseUrl;
  }

  setBaseUrl(url: string): void {
    this.baseUrl = url.replace(/\/$/, '');
    if (typeof chrome !== 'undefined' && chrome.storage?.local) {
      chrome.storage.local.set({ codexo_api_url: this.baseUrl });
    }
  }

  private restoreBaseUrl(): void {
    if (typeof chrome !== 'undefined' && chrome.storage?.local) {
      chrome.storage.local.get(['codexo_api_url'], (result) => {
        if (result?.['codexo_api_url']) {
          this.baseUrl = String(result['codexo_api_url']).replace(/\/$/, '');
        }
      });
    }
  }

  setToken(token: string, email?: string): void {
    this.token.set(token);
    if (email) {
      this.currentUser.set(email);
    }
    if (typeof chrome !== 'undefined' && chrome.storage?.local) {
      chrome.storage.local.set({ codexo_token: token, codexo_email: email });
    } else if (typeof localStorage !== 'undefined') {
      localStorage.setItem('codexo_token', token);
      if (email) localStorage.setItem('codexo_email', email);
    }
  }

  clearToken(): void {
    this.token.set(null);
    this.currentUser.set(null);
    if (typeof chrome !== 'undefined' && chrome.storage?.local) {
      chrome.storage.local.remove(['codexo_token', 'codexo_email']);
    } else if (typeof localStorage !== 'undefined') {
      localStorage.removeItem('codexo_token');
      localStorage.removeItem('codexo_email');
    }
  }

  private restoreToken(): void {
    if (typeof chrome !== 'undefined' && chrome.storage?.local) {
      chrome.storage.local.get(['codexo_token', 'codexo_email'], (result) => {
        if (result?.['codexo_token']) {
          this.token.set(String(result['codexo_token']));
          this.currentUser.set(String(result['codexo_email'] || 'User'));
        }
      });
    } else if (typeof localStorage !== 'undefined') {
      const stored = localStorage.getItem('codexo_token');
      if (stored) {
        this.token.set(stored);
        this.currentUser.set(localStorage.getItem('codexo_email') || 'User');
      }
    }
  }

  private getAuthHeaders(): HttpHeaders {
    let headers = new HttpHeaders({
      'Content-Type': 'application/json',
    });
    const currentToken = this.token();
    if (currentToken) {
      headers = headers.set('Authorization', `Bearer ${currentToken}`);
    }
    return headers;
  }

  // --- Auth Endpoints ---
  login(email: string, password: string): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.baseUrl}/api/auth/login`, { email, password })
      .pipe(
        tap((res) => {
          this.setToken(res.token, res.email);
        })
      );
  }

  register(email: string, password: string): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.baseUrl}/api/auth/register`, {
        email,
        password,
        confirmPassword: password,
      })
      .pipe(
        tap((res) => {
          this.setToken(res.token, res.email);
        })
      );
  }

  // --- Sessions & Intake ---
  getOrCreateSession(problem: ProblemContextPayload): Observable<ProblemSessionResponse> {
    return this.http.post<ProblemSessionResponse>(
      `${this.baseUrl}/api/sessions/context`,
      {
        platform: problem.platform,
        platformProblemId: problem.platformProblemId,
        title: problem.title,
        slug: problem.slug || problem.title.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '') || 'problem',
        url: problem.url,
        difficulty: problem.difficulty,
        description: problem.description,
      },
      { headers: this.getAuthHeaders() }
    );
  }

  // --- Hints ---
  getSessionHints(sessionId: string): Observable<HintResponseDto[]> {
    return this.http.get<HintResponseDto[]>(
      `${this.baseUrl}/api/sessions/${sessionId}/hints`,
      { headers: this.getAuthHeaders() }
    );
  }

  getHintStatus(sessionId: string): Observable<HintStatusDto> {
    return this.http.get<HintStatusDto>(
      `${this.baseUrl}/api/sessions/${sessionId}/hints/status`,
      { headers: this.getAuthHeaders() }
    );
  }

  requestNextHint(sessionId: string): Observable<HintResponseDto> {
    return this.http.post<HintResponseDto>(
      `${this.baseUrl}/api/sessions/${sessionId}/hints`,
      {},
      { headers: this.getAuthHeaders() }
    );
  }

  submitAttempt(
    sessionId: string,
    request: SubmitAttemptRequest
  ): Observable<SubmitAttemptResponse> {
    return this.http.post<SubmitAttemptResponse>(
      `${this.baseUrl}/api/sessions/${sessionId}/attempts`,
      {
        attemptText: request.attemptContent || (request as any).attemptText || '',
      },
      { headers: this.getAuthHeaders() }
    );
  }

  // --- Feedback ---
  submitFeedback(
    hintId: string,
    request: SubmitFeedbackRequest
  ): Observable<HintFeedbackResponse> {
    return this.http.post<HintFeedbackResponse>(
      `${this.baseUrl}/api/hints/${hintId}/feedback`,
      request,
      { headers: this.getAuthHeaders() }
    );
  }

  // --- Preferences ---
  getPreferences(): Observable<UserPreferenceDto> {
    return this.http.get<UserPreferenceDto>(`${this.baseUrl}/api/preferences`, {
      headers: this.getAuthHeaders(),
    });
  }

  updatePreferences(preferences: Partial<UserPreferenceDto>): Observable<UserPreferenceDto> {
    return this.http.put<UserPreferenceDto>(
      `${this.baseUrl}/api/preferences`,
      preferences,
      { headers: this.getAuthHeaders() }
    );
  }
}
