import { describe, it, expect } from 'vitest';
import * as fs from 'node:fs';
import * as path from 'node:path';
import {
  extractSlugFromUrl,
  cleanDescription,
  extractProblemFromDocument,
} from './leetcode-extractor';

describe('LeetCode Extractor', () => {
  describe('extractSlugFromUrl', () => {
    it('should extract slug from standard LeetCode URLs', () => {
      expect(extractSlugFromUrl('https://leetcode.com/problems/two-sum/')).toBe('two-sum');
      expect(extractSlugFromUrl('https://leetcode.com/problems/two-sum/description/')).toBe('two-sum');
      expect(extractSlugFromUrl('https://leetcode.com/problems/lru-cache/solutions/')).toBe('lru-cache');
      expect(extractSlugFromUrl('https://leetcode.cn/problems/reverse-linked-list/')).toBe('reverse-linked-list');
    });

    it('should return null for non-problem or non-LeetCode URLs', () => {
      expect(extractSlugFromUrl('https://leetcode.com/problemset/all/')).toBeNull();
      expect(extractSlugFromUrl('https://leetcode.com/contest/')).toBeNull();
      expect(extractSlugFromUrl('https://google.com/search?q=two-sum')).toBeNull();
      expect(extractSlugFromUrl('invalid-url')).toBeNull();
    });
  });

  describe('cleanDescription', () => {
    it('should remove UI buttons, svgs, and script tags while keeping text and code', () => {
      const container = document.createElement('div');
      container.innerHTML = `
        <p>Given an array <code>nums</code>.</p>
        <button class="copy-button shrink-0"><svg><path d="M0 0"/></svg>Copy</button>
        <script>console.log("malicious")</script>
        <pre><code>int x = 5;</code></pre>
      `;

      const cleaned = cleanDescription(container);
      expect(cleaned).toContain('Given an array nums.');
      expect(cleaned).toContain('int x = 5;');
      expect(cleaned).not.toContain('Copy');
      expect(cleaned).not.toContain('malicious');
    });
  });

  describe('extractProblemFromDocument (Two Sum Fixture)', () => {
    it('should extract title, problemId, difficulty, slug, and clean description from two-sum fixture', () => {
      const fixturePath = path.resolve(__dirname, 'fixtures/two-sum.html');
      const fixtureHtml = fs.readFileSync(fixturePath, 'utf-8');

      const doc = new DOMParser().parseFromString(fixtureHtml, 'text/html');
      const url = 'https://leetcode.com/problems/two-sum/description/';

      const result = extractProblemFromDocument(doc, url);

      expect(result).not.toBeNull();
      expect(result?.platform).toBe(1);
      expect(result?.platformProblemId).toBe('1');
      expect(result?.title).toBe('Two Sum');
      expect(result?.slug).toBe('two-sum');
      expect(result?.difficulty).toBe('Easy');
      expect(result?.url).toBe('https://leetcode.com/problems/two-sum/');
      expect(result?.description).toContain('Given an array of integers nums and an integer target');
      expect(result?.description).toContain('Constraints:');
      // Verify buttons/SVGs are not in the description
      expect(result?.description).not.toContain('Sign In');
      expect(result?.description).not.toContain('Copy');
    });
  });

  describe('extractProblemFromDocument (Hard Problem Alternative Layout Fixture)', () => {
    it('should extract hard difficulty and problem details with alternative class selectors', () => {
      const fixturePath = path.resolve(__dirname, 'fixtures/hard-problem.html');
      const fixtureHtml = fs.readFileSync(fixturePath, 'utf-8');

      const doc = new DOMParser().parseFromString(fixtureHtml, 'text/html');
      const url = 'https://leetcode.com/problems/median-of-two-sorted-arrays/';

      const result = extractProblemFromDocument(doc, url);

      expect(result).not.toBeNull();
      expect(result?.platform).toBe(1);
      expect(result?.platformProblemId).toBe('4');
      expect(result?.title).toBe('Median of Two Sorted Arrays');
      expect(result?.slug).toBe('median-of-two-sorted-arrays');
      expect(result?.difficulty).toBe('Hard');
      expect(result?.description).toContain('Given two sorted arrays nums1 and nums2');
      expect(result?.description).toContain('O(log (m+n))');
      expect(result?.description).not.toContain('Copy Code');
      expect(result?.description).not.toContain('Back');
    });
  });

  describe('extractProblemFromDocument (Edge Cases & Fallbacks)', () => {
    it('should return null if url is not a LeetCode problem', () => {
      const doc = new DOMParser().parseFromString('<div>Some text</div>', 'text/html');
      const result = extractProblemFromDocument(doc, 'https://leetcode.com/contest/');
      expect(result).toBeNull();
    });

    it('should fall back gracefully to document.title when DOM title element is absent', () => {
      const html = `
        <div data-track-load="description_content">
          <div class="text-yellow">Medium</div>
          <p>Description text here for fallback test with enough characters to pass minimum threshold.</p>
        </div>
      `;
      const doc = new DOMParser().parseFromString(html, 'text/html');
      doc.title = '15. 3Sum - LeetCode';

      const result = extractProblemFromDocument(doc, 'https://leetcode.com/problems/3sum/');
      expect(result).not.toBeNull();
      expect(result?.platformProblemId).toBe('15');
      expect(result?.title).toBe('3Sum');
      expect(result?.difficulty).toBe('Medium');
    });
  });
});
