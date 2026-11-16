import { describe, it, expect, beforeEach, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { App } from './app';
import { ExtensionBridgeService } from './core/services/extension-bridge.service';

describe('App Component', () => {
  beforeEach(async () => {
    (globalThis as any).chrome = {
      runtime: {
        sendMessage: vi.fn((_msg, cb) => cb?.({ success: true })),
        onMessage: { addListener: vi.fn() },
      },
    };

    await TestBed.configureTestingModule({
      imports: [App],
      providers: [ExtensionBridgeService],
    }).compileComponents();
  });

  it('should create the app and render header title', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
    expect(app.title).toEqual('Codexo');
  });

  it('should render empty state when no problem is detected', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.empty-title')?.textContent).toContain('No Problem Detected');
  });
});
