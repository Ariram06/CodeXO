import { Injectable, signal } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import {
  AuthResponse,
  ChatMessageResponse,
  MessageFeedbackResponse,
  SendMessageRequest,
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

  // --- Chat Endpoints ---
  sendMessage(request: SendMessageRequest): Observable<ChatMessageResponse> {
    return this.http.post<ChatMessageResponse>(
      `${this.baseUrl}/api/chat/messages`,
      request,
      { headers: this.getAuthHeaders() }
    );
  }

  // --- Feedback ---
  submitFeedback(
    messageId: string,
    request: SubmitFeedbackRequest
  ): Observable<MessageFeedbackResponse> {
    return this.http.post<MessageFeedbackResponse>(
      `${this.baseUrl}/api/messages/${messageId}/feedback`,
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
