import { Injectable, inject } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { Marked, Tokens, RendererObject } from 'marked';
import DOMPurify from 'dompurify';
import katex from 'katex';
import Prism from 'prismjs';

import 'prismjs/components/prism-c';
import 'prismjs/components/prism-cpp';
import 'prismjs/components/prism-csharp';
import 'prismjs/components/prism-java';
import 'prismjs/components/prism-python';
import 'prismjs/components/prism-typescript';
import 'prismjs/components/prism-go';
import 'prismjs/components/prism-rust';
import 'prismjs/components/prism-sql';
import 'prismjs/components/prism-bash';
import 'prismjs/components/prism-json';

@Injectable({
  providedIn: 'root',
})
export class MarkdownRendererService {
  private sanitizer = inject(DomSanitizer);
  private marked: Marked;

  constructor() {
    this.marked = new Marked({
      gfm: true,
      breaks: true,
    });

    this.configureMarkedRenderer();
  }

  private configureMarkedRenderer(): void {
    const renderer: RendererObject = {
      code: ({ text, lang }: Tokens.Code) => {
        const rawLang = (lang || '').trim().toLowerCase();
        const normalizedLang = this.normalizeLanguage(rawLang);
        let highlighted = '';

        const grammar = Prism.languages[normalizedLang];
        if (grammar) {
          try {
            highlighted = Prism.highlight(text, grammar, normalizedLang);
          } catch {
            highlighted = this.escapeHtml(text);
          }
        } else {
          highlighted = this.escapeHtml(text);
        }

        const displayLang = normalizedLang === 'text' ? 'CODE' : normalizedLang.toUpperCase();
        const encodedCode = encodeURIComponent(text);

        return `<div class="code-block-wrapper">
  <div class="code-header">
    <span class="code-lang">${displayLang}</span>
    <button type="button" class="copy-code-btn" data-code="${encodedCode}">Copy</button>
  </div>
  <pre class="code-pre"><code class="language-${normalizedLang}">${highlighted}</code></pre>
</div>`;
      },

      codespan: ({ text }: Tokens.Codespan) => {
        return `<code class="inline-code">${this.escapeHtml(text)}</code>`;
      },

      link: ({ href, title, text }: Tokens.Link) => {
        const cleanHref = href.startsWith('http://') || href.startsWith('https://') ? href : '#';
        const titleAttr = title ? ` title="${this.escapeHtml(title)}"` : '';
        return `<a href="${cleanHref}" target="_blank" rel="noopener noreferrer"${titleAttr}>${text}</a>`;
      },

      table(this: any, token: Tokens.Table) {
        let t = '';
        let n = '';
        for (let s = 0; s < token.header.length; s++) {
          n += this.tablecell(token.header[s]);
        }
        t += this.tablerow({ text: n });
        let r = '';
        for (let s = 0; s < token.rows.length; s++) {
          const o = token.rows[s];
          n = '';
          for (let i = 0; i < o.length; i++) {
            n += this.tablecell(o[i]);
          }
          r += this.tablerow({ text: n });
        }
        const body = r ? `<tbody>\n${r}</tbody>` : '';
        return `<div class="table-wrapper"><table>\n<thead>\n${t}</thead>\n${body}\n</table></div>\n`;
      },
    };

    this.marked.use({ renderer });
  }

  private normalizeLanguage(lang: string): string {
    if (!lang) return 'text';
    const map: Record<string, string> = {
      py: 'python',
      python3: 'python',
      js: 'javascript',
      ts: 'typescript',
      'c++': 'cpp',
      cs: 'csharp',
      'c#': 'csharp',
      golang: 'go',
      sh: 'bash',
      shell: 'bash',
      zsh: 'bash',
    };
    return map[lang] || lang;
  }

  private escapeHtml(str: string): string {
    return str
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
  }

  /**
   * Pre-renders LaTeX math expressions with KaTeX, converts markdown with marked,
   * and sanitizes using DOMPurify under strict CSP constraints.
   */
  renderToSafeHtml(content: string): SafeHtml {
    if (!content) {
      return this.sanitizer.bypassSecurityTrustHtml('');
    }

    // 1. Process math expressions with KaTeX
    const processedWithMath = this.renderMath(content);

    // 2. Parse Markdown
    const rawHtml = this.marked.parse(processedWithMath) as string;

    // 3. Sanitize HTML with DOMPurify
    const cleanHtml = this.sanitize(rawHtml);

    return this.sanitizer.bypassSecurityTrustHtml(cleanHtml);
  }

  /**
   * Replaces LaTeX math syntax with KaTeX HTML output.
   * Supports block math ($$ ... $$) and inline math ($ ... $).
   */
  renderMath(text: string): string {
    if (!text.includes('$')) {
      return text;
    }

    // First replace block math: $$ math $$
    let result = text.replace(/\$\$([\s\S]+?)\$\$/g, (_, math) => {
      try {
        return katex.renderToString(math.trim(), {
          displayMode: true,
          throwOnError: false,
        });
      } catch {
        return `$$${math}$$`;
      }
    });

    // Then replace inline math: $ math $ (ignoring escaped \$ or standalone $)
    // Exclude cases where $ is followed immediately by digit and space/punctuation (currency)
    result = result.replace(/(?<!\\)\$([^\$\n\r]+?)\$(?!\d)/g, (match, math) => {
      // Avoid converting common non-math patterns like $100 or empty $
      const trimmed = math.trim();
      if (!trimmed || /^\d+(\.\d+)?$/.test(trimmed)) {
        return match;
      }

      try {
        return katex.renderToString(trimmed, {
          displayMode: false,
          throwOnError: false,
        });
      } catch {
        return match;
      }
    });

    return result;
  }

  private sanitize(html: string): string {
    if (typeof DOMPurify?.sanitize === 'function') {
      return DOMPurify.sanitize(html, {
        ADD_TAGS: [
          'span',
          'div',
          'pre',
          'code',
          'button',
          'table',
          'thead',
          'tbody',
          'tr',
          'th',
          'td',
          // KaTeX tags
          'math',
          'semantics',
          'mrow',
          'mi',
          'mo',
          'mn',
          'annotation',
        ],
        ADD_ATTR: ['class', 'data-code', 'data-lang', 'target', 'rel', 'aria-hidden'],
        ALLOW_DATA_ATTR: true,
      });
    }
    return html;
  }
}
