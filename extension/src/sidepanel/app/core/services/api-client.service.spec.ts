import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ApiClientService } from './api-client.service';
import { SendMessageRequest } from '../models/api-contracts';

describe('ApiClientService - Streaming and Fallback', () => {
  let service: ApiClientService;
  let httpMock: HttpTestingController;
  let originalFetch: typeof globalThis.fetch;

  beforeEach(() => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [ApiClientService],
    });

    service = TestBed.inject(ApiClientService);
    httpMock = TestBed.inject(HttpTestingController);
    service.setToken('test-bearer-token', 'test@codexo.dev');

    originalFetch = globalThis.fetch;
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
    httpMock.verify();
  });

  it('streamMessage should send POST with Authorization header and process SSE chunks', async () => {
    const sseLines = [
      'data: {"conversationId":"c-1","messageId":"m-1","delta":"Hello ","isDone":false,"model":"gemini-3.5-flash"}\n\n',
      'data: {"conversationId":"c-1","messageId":"m-1","delta":"world!","isDone":false,"model":"gemini-3.5-flash"}\n\n',
      'data: [DONE]\n\n',
    ];

    const encoder = new TextEncoder();
    const stream = new ReadableStream<Uint8Array>({
      start(controller) {
        for (const line of sseLines) {
          controller.enqueue(encoder.encode(line));
        }
        controller.close();
      },
    });

    let capturedHeaders: any;
    globalThis.fetch = vi.fn(async (url: any, init: any) => {
      capturedHeaders = init?.headers;
      return new Response(stream, {
        status: 200,
        headers: { 'Content-Type': 'text/event-stream' },
      });
    });

    const receivedDeltas: string[] = [];
    const request: SendMessageRequest = {
      message: 'Say hello',
    };

    const result = await service.streamMessage(request, (delta) => {
      receivedDeltas.push(delta);
    });

    expect(capturedHeaders['Authorization']).toBe('Bearer test-bearer-token');
    expect(receivedDeltas).toEqual(['Hello ', 'world!']);
    expect(result.conversationId).toBe('c-1');
    expect(result.messageId).toBe('m-1');
    expect(result.fullContent).toBe('Hello world!');
    expect(result.model).toBe('gemini-3.5-flash');
  });

  it('streamMessage should stop when AbortSignal is triggered', async () => {
    const encoder = new TextEncoder();
    const abortController = new AbortController();

    const stream = new ReadableStream<Uint8Array>({
      start(controller) {
        controller.enqueue(
          encoder.encode('data: {"conversationId":"c-abort","messageId":"m-a","delta":"Partial","isDone":false}\n\n')
        );
      },
    });

    globalThis.fetch = vi.fn(async (_url, _init) => {
      return new Response(stream, { status: 200 });
    });

    const deltas: string[] = [];
    const streamPromise = service.streamMessage(
      { message: 'Cancel me' },
      (d) => {
        deltas.push(d);
        abortController.abort();
      },
      abortController.signal
    );

    const result = await streamPromise;
    expect(deltas).toEqual(['Partial']);
    expect(result.fullContent).toBe('Partial');
  });

  it('streamMessage should fallback to sendMessage when streaming connection fails', async () => {
    globalThis.fetch = vi.fn(async () => {
      throw new Error('Connection refused');
    });

    const request: SendMessageRequest = {
      message: 'Fallback test',
    };

    const deltas: string[] = [];
    const streamPromise = service.streamMessage(request, (d) => deltas.push(d));

    // Yield microtask so fetch error handler triggers fallbackToNonStreaming
    await Promise.resolve();
    await Promise.resolve();

    // Resolve the fallback HTTP call
    const req = httpMock.expectOne('http://localhost:5275/api/chat/messages');
    expect(req.request.method).toBe('POST');
    req.flush({
      conversationId: 'fallback-conv',
      messageId: 'fallback-msg',
      role: 'assistant',
      content: 'Fallback non-streaming response',
      createdAtUtc: new Date().toISOString(),
      model: 'fallback-model',
    });

    const result = await streamPromise;
    expect(result.fullContent).toBe('Fallback non-streaming response');
    expect(deltas).toEqual(['Fallback non-streaming response']);
    expect(result.conversationId).toBe('fallback-conv');
  });

  it('streamMessage should throw on HTTP 429 error from streaming endpoint', async () => {
    globalThis.fetch = vi.fn(async () => {
      return new Response(
        JSON.stringify({ message: 'Rate limit exceeded.', retryAfterSeconds: 30 }),
        { status: 429, headers: { 'Content-Type': 'application/json' } }
      );
    });

    await expect(
      service.streamMessage({ message: 'Trigger 429' }, () => {})
    ).rejects.toMatchObject({
      status: 429,
      message: expect.stringContaining('Rate limit exceeded'),
    });
  });
});
