import { describe, it, expect, beforeEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { MarkdownRendererService } from './markdown-renderer.service';
import { DomSanitizer } from '@angular/platform-browser';

describe('MarkdownRendererService', () => {
  let service: MarkdownRendererService;

  beforeEach(() => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [MarkdownRendererService],
    });

    service = TestBed.inject(MarkdownRendererService);
  });

  it('should render standard Markdown elements (headings, bold, lists, links)', () => {
    const markdown = `# Two Sum Solution
Here is **bold** text and an *italic* note.
- Step 1: Initialize hash map
- Step 2: Iterate array
[LeetCode](https://leetcode.com)`;

    const safeHtml = service.renderToSafeHtml(markdown);
    const html = (safeHtml as any).changingThisBreaksApplicationSecurity;

    expect(html).toContain('<h1>Two Sum Solution</h1>');
    expect(html).toContain('<strong>bold</strong>');
    expect(html).toContain('<em>italic</em>');
    expect(html).toContain('<ul>');
    expect(html).toContain('<li>Step 1: Initialize hash map</li>');
    expect(html).toContain('<a href="https://leetcode.com" target="_blank" rel="noopener noreferrer">LeetCode</a>');
  });

  it('should render code blocks with language badge, Prism highlighting, and Copy button', () => {
    const markdown = `\`\`\`python
def two_sum(nums, target):
    lookup = {}
    return []
\`\`\``;

    const safeHtml = service.renderToSafeHtml(markdown);
    const html = (safeHtml as any).changingThisBreaksApplicationSecurity;

    expect(html).toContain('class="code-block-wrapper"');
    expect(html).toContain('class="code-lang">PYTHON</span>');
    expect(html).toContain('class="copy-code-btn"');
    expect(html).toContain('data-code="');

    // Verify Prism tokens
    expect(html).toContain('class="token keyword">def</span>');
    expect(html).toContain('class="token keyword">return</span>');

    // Verify data-code is correctly encoded
    const match = html.match(/data-code="([^"]+)"/);
    expect(match).not.toBeNull();
    const decoded = decodeURIComponent(match![1]);
    expect(decoded).toContain('def two_sum(nums, target):');
  });

  it('should render LaTeX math expressions using KaTeX', () => {
    const markdown = 'The time complexity is $O(n)$ and space complexity is $O(n)$.';

    const safeHtml = service.renderToSafeHtml(markdown);
    const html = (safeHtml as any).changingThisBreaksApplicationSecurity;

    expect(html).toContain('class="katex"');
    expect(html).toContain('class="katex-html"');
    expect(html).not.toContain('$O(n)$');
  });

  it('should render block math ($$ ... $$) with displayMode', () => {
    const markdown = `Calculation:
$$
T(n) = 2T(n/2) + O(n)
$$`;

    const safeHtml = service.renderToSafeHtml(markdown);
    const html = (safeHtml as any).changingThisBreaksApplicationSecurity;

    expect(html).toContain('class="katex-display"');
  });

  it('should strictly sanitize malicious HTML and disarm XSS vectors', () => {
    const malicious = `
# Safe Title
<script>alert('XSS')</script>
<img src="invalid-image" onerror="alert('pwned')">
<a href="javascript:alert('steal_cookie')">Click here</a>
`;

    const safeHtml = service.renderToSafeHtml(malicious);
    const html = (safeHtml as any).changingThisBreaksApplicationSecurity;

    // Script tags are removed
    expect(html).not.toContain('<script>');
    expect(html).not.toContain("alert('XSS')");

    // onerror handlers are stripped
    expect(html).not.toContain('onerror');

    // javascript: links are stripped / disarmed
    expect(html).not.toContain('href="javascript:');
  });

  it('should return empty SafeHtml for empty or null input', () => {
    const safeHtml = service.renderToSafeHtml('');
    const html = (safeHtml as any).changingThisBreaksApplicationSecurity;
    expect(html).toBe('');
  });
});
