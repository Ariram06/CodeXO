import { describe, it, expect, beforeEach } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AttemptSubmissionComponent } from './attempt-submission.component';

describe('AttemptSubmissionComponent', () => {
  let component: AttemptSubmissionComponent;
  let fixture: ComponentFixture<AttemptSubmissionComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AttemptSubmissionComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(AttemptSubmissionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should initialize with an invalid state and disabled submit button', () => {
    expect(component.isValid()).toBe(false);
    const submitBtn = fixture.nativeElement.querySelector('.submit-button') as HTMLButtonElement;
    expect(submitBtn.disabled).toBe(true);
  });

  it('should reject short attempts (< 10 chars)', () => {
    component.onContentChange('too short');
    fixture.detectChanges();
    expect(component.isValid()).toBe(false);
    expect(component.validationError()).toContain('at least 10 characters');
  });

  it('should reject requests asking for the full solution code', () => {
    component.onContentChange('please give me the full complete solution code for this problem');
    fixture.detectChanges();
    expect(component.isValid()).toBe(false);
    expect(component.validationError()).toContain('Codexo gives progressive hints');
  });

  it('should reject repetitive junk submissions', () => {
    component.onContentChange('aaaaaaaaaaaa');
    fixture.detectChanges();
    expect(component.isValid()).toBe(false);
    expect(component.validationError()).toContain('genuine problem-solving attempt');
  });

  it('should approve a thoughtfulDSA attempt description and emit event', () => {
    let emittedPayload: any = null;
    component.attemptSubmitted.subscribe((payload) => {
      emittedPayload = payload;
    });

    const validText = 'I plan to use a two-pointer technique starting at both ends of the sorted array.';
    component.onContentChange(validText);
    component.attemptContent = validText;
    fixture.detectChanges();

    expect(component.isValid()).toBe(true);
    expect(component.validationError()).toBeNull();

    component.onSubmit();
    expect(emittedPayload).not.toBeNull();
    expect(emittedPayload.attemptContent).toBe(validText);
  });
});
