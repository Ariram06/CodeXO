import { describe, it, expect, beforeEach } from 'vitest';
import { extractPageContext, findMatchingAdapter } from './platforms.config';
import { REDACTED_MASK } from '../shared/secret-redactor';

describe('PlatformsConfig & Adapters', () => {
  let doc: Document;

  beforeEach(() => {
    doc = document.implementation.createHTMLDocument('Sample Title');
  });

  describe('Adapter Matching', () => {
    it('should identify supported platforms by URL host', () => {
      expect(findMatchingAdapter('https://leetcode.com/problems/two-sum')?.name).toBe('LeetCode');
      expect(findMatchingAdapter('https://codeforces.com/contest/1/problem/A')?.name).toBe('Codeforces');
      expect(findMatchingAdapter('https://www.hackerrank.com/challenges/simple-array-sum')?.name).toBe('HackerRank');
      expect(findMatchingAdapter('https://www.geeksforgeeks.org/problems/subarray-with-given-sum')?.name).toBe('GeeksforGeeks');
      expect(findMatchingAdapter('https://www.codechef.com/problems/TEST')?.name).toBe('CodeChef');
      expect(findMatchingAdapter('https://github.com/microsoft/vscode')).toBeNull();
    });
  });

  describe('LeetCode Adapter', () => {
    it('should extract title, problem statement, language and error', () => {
      const url = 'https://leetcode.com/problems/two-sum/';
      doc.body.innerHTML = `
        <div data-cy="question-title">1. Two Sum</div>
        <div data-track-load="description_content">
          <p>Given an array of integers <code>nums</code> and an integer <code>target</code>.</p>
          <button class="copy-button">Copy</button>
        </div>
        <button data-cy="lang-select">Python3</button>
        <div data-e2e-locator="console-result">Runtime Error: Index out of range</div>
      `;

      const result = extractPageContext(doc, url);
      expect(result.platform).toBe('LeetCode');
      expect(result.title).toBe('1. Two Sum');
      expect(result.problemText).toContain('Given an array of integers nums and an integer target.');
      expect(result.problemText).not.toContain('Copy');
      expect(result.language).toBe('Python3');
      expect(result.errorText).toBe('Runtime Error: Index out of range');
    });
  });

  describe('Codeforces Adapter', () => {
    it('should extract Codeforces problem title, statement, and selected language', () => {
      const url = 'https://codeforces.com/problemset/problem/4/A';
      doc.body.innerHTML = `
        <div class="problem-statement">
          <div class="header">
            <div class="title">A. Watermelon</div>
          </div>
          <div>One hot summer day Pete and his friend Billy decided to buy a watermelon.</div>
        </div>
        <select name="programTypeId">
          <option value="54">GNU C++17</option>
          <option value="73" selected>GNU C++20 (64)</option>
        </select>
        <div class="verdict-rejected">Wrong answer on test 1</div>
      `;

      const result = extractPageContext(doc, url);
      expect(result.platform).toBe('Codeforces');
      expect(result.title).toBe('A. Watermelon');
      expect(result.problemText).toContain('Watermelon');
      expect(result.problemText).toContain('One hot summer day');
      expect(result.language).toBe('GNU C++20 (64)');
      expect(result.errorText).toBe('Wrong answer on test 1');
    });
  });

  describe('HackerRank Adapter', () => {
    it('should extract HackerRank challenge title, statement, and language', () => {
      const url = 'https://www.hackerrank.com/challenges/solve-me-first';
      doc.body.innerHTML = `
        <h1 class="ui-heading challenge-title">Solve Me First</h1>
        <div class="challenge-body-html">
          <p>Complete the function solveMeFirst to compute the sum of two integers.</p>
        </div>
        <div class="select-language">
          <div class="css-1uccc91-singleValue">Java 15</div>
        </div>
      `;

      const result = extractPageContext(doc, url);
      expect(result.platform).toBe('HackerRank');
      expect(result.title).toBe('Solve Me First');
      expect(result.problemText).toContain('Complete the function solveMeFirst');
      expect(result.language).toBe('Java 15');
    });
  });

  describe('GeeksforGeeks Adapter', () => {
    it('should extract GeeksforGeeks problem title and statement', () => {
      const url = 'https://www.geeksforgeeks.org/problems/kadanes-algorithm';
      doc.body.innerHTML = `
        <div class="problem-tab__title">Kadane's Algorithm</div>
        <div class="problems_problem_content__X2dig">
          <p>Given an integer array arr[]. Find the maximum sum subarray.</p>
        </div>
        <div class="dropdown-toggle language-select">C++</div>
      `;

      const result = extractPageContext(doc, url);
      expect(result.platform).toBe('GeeksforGeeks');
      expect(result.title).toBe("Kadane's Algorithm");
      expect(result.problemText).toContain('maximum sum subarray');
      expect(result.language).toBe('C++');
    });
  });

  describe('CodeChef Adapter', () => {
    it('should extract CodeChef problem code, statement, and language', () => {
      const url = 'https://www.codechef.com/problems/START01';
      doc.body.innerHTML = `
        <h1 class="_title" id="problem-code">START01</h1>
        <div id="problem-statement">
          <p>Write a program that accepts a number, n, and outputs the same.</p>
        </div>
        <div class="select-language">Python 3</div>
      `;

      const result = extractPageContext(doc, url);
      expect(result.platform).toBe('CodeChef');
      expect(result.title).toBe('START01');
      expect(result.problemText).toContain('outputs the same');
      expect(result.language).toBe('Python 3');
    });
  });

  describe('Generic Web Fallback', () => {
    it('should extract title and main text from unsupported generic website', () => {
      const url = 'https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference';
      doc.title = 'JavaScript Reference - MDN';
      doc.body.innerHTML = `
        <main>
          <article>
            <h1>JavaScript Guide</h1>
            <p>JavaScript is a lightweight interpreted compiled programming language with first-class functions.</p>
          </article>
        </main>
      `;

      const result = extractPageContext(doc, url);
      expect(result.platform).toBe('Web');
      expect(result.title).toBe('JavaScript Reference - MDN');
      expect(result.problemText).toContain('JavaScript is a lightweight');
    });

    it('should redact secrets if found in problem text', () => {
      const url = 'https://internal-docs.local/api';
      doc.body.innerHTML = `
        <main>
          <p>Connect with apiKey = "sk-abcdef1234567890abcdef1234567890"</p>
        </main>
      `;

      const result = extractPageContext(doc, url);
      expect(result.problemText).not.toContain('sk-abcdef');
      expect(result.problemText).toContain(REDACTED_MASK);
    });
  });
});
