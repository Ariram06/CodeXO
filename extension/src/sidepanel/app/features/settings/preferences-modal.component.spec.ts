import { describe, it, expect, beforeEach } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PreferencesModalComponent } from './preferences-modal.component';

describe('PreferencesModalComponent', () => {
  let component: PreferencesModalComponent;
  let fixture: ComponentFixture<PreferencesModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PreferencesModalComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(PreferencesModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should not render modal DOM when isOpen is false', () => {
    fixture.componentRef.setInput('isOpen', false);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.modal-backdrop')).toBeNull();
  });

  it('should render modal and display active model', () => {
    fixture.componentRef.setInput('isOpen', true);
    fixture.componentRef.setInput('preferences', {
      preferredLanguage: 'Python',
      autoUnlockL1: false,
      reasoningLevel: 'medium',
      model: 'gemini-3.5-flash-lite',
      updatedAt: new Date().toISOString(),
    });
    fixture.detectChanges();

    const badge = fixture.nativeElement.querySelector('.model-badge');
    expect(badge).not.toBeNull();
    expect(badge.textContent).toContain('gemini-3.5-flash-lite');
  });

  it('should emit saved preferences including reasoningLevel when submitted', () => {
    fixture.componentRef.setInput('isOpen', true);
    fixture.detectChanges();

    let emittedPrefs: any = null;
    component.preferencesSaved.subscribe((prefs) => {
      emittedPrefs = prefs;
    });

    component.selectedLanguage = 'Python';
    component.autoUnlockL1 = false;
    component.reasoningLevel = 'high';
    component.onSave();

    expect(emittedPrefs).not.toBeNull();
    expect(emittedPrefs.preferredLanguage).toBe('Python');
    expect(emittedPrefs.autoUnlockL1).toBe(false);
    expect(emittedPrefs.reasoningLevel).toBe('high');
  });

  it('should show confirmation and emit clearAllHistory when confirmed', () => {
    fixture.componentRef.setInput('isOpen', true);
    fixture.detectChanges();

    let cleared = false;
    component.clearAllHistory.subscribe(() => {
      cleared = true;
    });

    expect(component.showClearConfirm()).toBe(false);
    const clearBtn = fixture.nativeElement.querySelector('.clear-history-btn');
    clearBtn.click();
    fixture.detectChanges();

    expect(component.showClearConfirm()).toBe(true);
    const confirmBtn = fixture.nativeElement.querySelector('.confirm-delete-btn');
    confirmBtn.click();
    fixture.detectChanges();

    expect(cleared).toBe(true);
    expect(component.showClearConfirm()).toBe(false);
  });
});
