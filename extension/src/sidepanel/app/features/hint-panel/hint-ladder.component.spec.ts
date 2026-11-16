import { describe, it, expect, beforeEach } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HintLadderComponent } from './hint-ladder.component';
import { HintResponseDto } from '../../core/models/api-contracts';

describe('HintLadderComponent', () => {
  let component: HintLadderComponent;
  let fixture: ComponentFixture<HintLadderComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HintLadderComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(HintLadderComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should render all 5 ladder level definitions', () => {
    expect(component.levels.length).toBe(5);
    const levelCards = fixture.nativeElement.querySelectorAll('.level-card');
    expect(levelCards.length).toBe(5);
  });

  it('should display unlocked content for available hints', () => {
    const mockHints: HintResponseDto[] = [
      {
        hintId: 'h-1',
        level: 1,
        levelName: 'L1_Understand',
        content: 'Understand the two-sum problem input array.',
        observations: ['Array contains positive and negative integers', 'Target may be negative'],
        guidingQuestions: ['What if no pair exists?'],
        keyTakeaway: 'Always observe array bounds first.',
        createdAt: new Date().toISOString(),
      },
    ];

    fixture.componentRef.setInput('unlockedHints', mockHints);
    fixture.detectChanges();

    expect(component.unlockedCount()).toBe(1);
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Understand the two-sum problem input array.');
    expect(compiled.textContent).toContain('Array contains positive and negative integers');
    expect(compiled.textContent).toContain('Always observe array bounds first.');
  });

  it('should emit feedback when thumbs up or thumbs down is clicked', () => {
    const mockHints: HintResponseDto[] = [
      {
        hintId: 'h-1',
        level: 1,
        levelName: 'L1_Understand',
        content: 'Hint content',
        createdAt: new Date().toISOString(),
      },
    ];

    fixture.componentRef.setInput('unlockedHints', mockHints);
    fixture.detectChanges();

    let emittedFeedback: any = null;
    component.feedbackSubmitted.subscribe((fb) => {
      emittedFeedback = fb;
    });

    const thumbUpBtn = fixture.nativeElement.querySelector('.thumb-btn') as HTMLButtonElement;
    thumbUpBtn.click();
    fixture.detectChanges();

    expect(emittedFeedback).not.toBeNull();
    expect(emittedFeedback.hintId).toBe('h-1');
    expect(emittedFeedback.isHelpful).toBe(true);
  });
});
