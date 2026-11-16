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

  it('should render modal and emit saved preferences when submitted', () => {
    fixture.componentRef.setInput('isOpen', true);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.modal-backdrop')).not.toBeNull();

    let emittedPrefs: any = null;
    component.preferencesSaved.subscribe((prefs) => {
      emittedPrefs = prefs;
    });

    component.selectedLanguage = 'Python';
    component.autoUnlockL1 = false;
    component.onSave();

    expect(emittedPrefs).not.toBeNull();
    expect(emittedPrefs.preferredLanguage).toBe('Python');
    expect(emittedPrefs.autoUnlockL1).toBe(false);
  });
});
