// Codexo Unified Platform Adapters Config
// Centralized configuration for LeetCode, Codeforces, HackerRank, GeeksforGeeks, CodeChef,
// and Generic Web Fallback.

import { redactSecrets } from '../shared/secret-redactor';

export interface ExtractedPageData {
  platform: string;
  url: string;
  title: string;
  problemText?: string;
  language?: string;
  errorText?: string;
  selection?: string;
}

export interface PlatformAdapter {
  name: string;
  hostPattern: RegExp;
  extractTitle: (doc: Document, url: string) => string | null;
  extractProblemText: (doc: Document) => string | null;
  extractLanguage: (doc: Document) => string | null;
  extractErrorText?: (doc: Document) => string | null;
}

export function cleanElementText(container: Element | null): string | null {
  if (!container) return null;

  const clone = container.cloneNode(true) as Element;
  const removeSelectors = [
    'script',
    'style',
    'button',
    'svg',
    'nav',
    'header',
    'footer',
    '[role="button"]',
    '.copy-button',
    '.shrink-0',
  ];

  removeSelectors.forEach((sel) => {
    clone.querySelectorAll(sel).forEach((el) => el.remove());
  });

  const raw = clone.textContent || '';
  const cleaned = raw
    .replace(/\r\n/g, '\n')
    .replace(/[ \t]+/g, ' ')
    .replace(/\n{3,}/g, '\n\n')
    .trim();

  return cleaned.length > 0 ? cleaned : null;
}

export const PLATFORM_ADAPTERS: PlatformAdapter[] = [
  // 1. LeetCode
  {
    name: 'LeetCode',
    hostPattern: /leetcode\.(com|cn)/i,
    extractTitle: (doc, url) => {
      const el = doc.querySelector('div[data-cy="question-title"], .text-title-large, a[href*="/problems/"], h4');
      if (el?.textContent?.trim()) return el.textContent.trim();
      const match = url.match(/\/problems\/([^/]+)/);
      if (match) {
        return match[1]
          .split('-')
          .map((s) => s.charAt(0).toUpperCase() + s.slice(1))
          .join(' ');
      }
      return doc.title.replace(/ - LeetCode.*/i, '').trim() || null;
    },
    extractProblemText: (doc) => {
      const container = doc.querySelector(
        '[data-track-load="description_content"], .elfjS, .question-content__JfgR, .content__u3I1'
      );
      return cleanElementText(container);
    },
    extractLanguage: (doc) => {
      const btn = doc.querySelector('button[id*="headlessui-listbox-button"], [data-cy="lang-select"]');
      return btn?.textContent?.trim() || null;
    },
    extractErrorText: (doc) => {
      const el = doc.querySelector('[data-e2e-locator="console-result"], .testcase-result, .bg-red-500');
      return cleanElementText(el);
    },
  },

  // 2. Codeforces
  {
    name: 'Codeforces',
    hostPattern: /codeforces\.com/i,
    extractTitle: (doc) => {
      const el = doc.querySelector('.problem-statement .header .title');
      return el?.textContent?.trim() || null;
    },
    extractProblemText: (doc) => {
      const container = doc.querySelector('.problem-statement');
      return cleanElementText(container);
    },
    extractLanguage: (doc) => {
      const opt = doc.querySelector<HTMLOptionElement>('select[name="programTypeId"] option:checked');
      return opt?.textContent?.trim() || null;
    },
    extractErrorText: (doc) => {
      const el = doc.querySelector('.verdict-rejected, .verdict-fail');
      return cleanElementText(el);
    },
  },

  // 3. HackerRank
  {
    name: 'HackerRank',
    hostPattern: /hackerrank\.com/i,
    extractTitle: (doc) => {
      const el = doc.querySelector('.page-label, .challenge-title, h1.ui-heading, .challenge-name');
      return el?.textContent?.trim() || null;
    },
    extractProblemText: (doc) => {
      const container = doc.querySelector('.challenge-body-html, .problem-statement, .challenge-content');
      return cleanElementText(container);
    },
    extractLanguage: (doc) => {
      const el = doc.querySelector('.select-language .css-1uccc91-singleValue, .language-selector');
      return el?.textContent?.trim() || null;
    },
    extractErrorText: (doc) => {
      const el = doc.querySelector('.compile-error, .testcase-error, .compiler-message');
      return cleanElementText(el);
    },
  },

  // 4. GeeksforGeeks
  {
    name: 'GeeksforGeeks',
    hostPattern: /geeksforgeeks\.org/i,
    extractTitle: (doc) => {
      const el = doc.querySelector('.problem-tab__title, .problems_header_content__title, .gfg-header');
      return el?.textContent?.trim() || null;
    },
    extractProblemText: (doc) => {
      const container = doc.querySelector(
        '.problems_problem_content__X2dig, .problem-statement, .problemQuestion, .article-content'
      );
      return cleanElementText(container);
    },
    extractLanguage: (doc) => {
      const el = doc.querySelector('.dropdown-toggle.language-select, .language-btn');
      return el?.textContent?.trim() || null;
    },
    extractErrorText: (doc) => {
      const el = doc.querySelector('.compilation-error, .error-message, .terminal-error');
      return cleanElementText(el);
    },
  },

  // 5. CodeChef
  {
    name: 'CodeChef',
    hostPattern: /codechef\.com/i,
    extractTitle: (doc) => {
      const el = doc.querySelector('#problem-code, .problem-name, h1._title');
      return el?.textContent?.trim() || null;
    },
    extractProblemText: (doc) => {
      const container = doc.querySelector('#problem-statement, .problem-statement, ._problemBody');
      return cleanElementText(container);
    },
    extractLanguage: (doc) => {
      const el = doc.querySelector('.select-language, #custom-language, .language-picker');
      return el?.textContent?.trim() || null;
    },
    extractErrorText: (doc) => {
      const el = doc.querySelector('.run-error, .error-output, .status-failed');
      return cleanElementText(el);
    },
  },
];

export function findMatchingAdapter(url: string): PlatformAdapter | null {
  try {
    const parsed = new URL(url);
    return PLATFORM_ADAPTERS.find((adapter) => adapter.hostPattern.test(parsed.hostname)) || null;
  } catch {
    return null;
  }
}

export function extractPageContext(doc: Document, url: string): ExtractedPageData {
  const adapter = findMatchingAdapter(url);
  const platform = adapter ? adapter.name : 'Web';

  let rawTitle = adapter ? adapter.extractTitle(doc, url) : null;
  if (!rawTitle) {
    rawTitle = doc.title?.trim() || 'Untitled Page';
  }

  let rawProblemText = adapter ? adapter.extractProblemText(doc) : null;
  if (!rawProblemText && platform === 'Web') {
    const mainEl = doc.querySelector('main, article, #content, .content, body');
    rawProblemText = cleanElementText(mainEl);
  }

  const rawLanguage = adapter ? adapter.extractLanguage(doc) : null;
  const rawError = adapter?.extractErrorText ? adapter.extractErrorText(doc) : null;

  // Selected text on the page
  let selectionText: string | null = null;
  try {
    const sel = doc.getSelection ? doc.getSelection()?.toString() : null;
    if (sel && sel.trim().length > 0) {
      selectionText = sel.trim().slice(0, 4000);
    }
  } catch {
    // Ignore selection error
  }

  // Redact secrets and truncate per limits
  const title = redactSecrets(rawTitle) || 'Untitled Page';
  const problemText = rawProblemText ? redactSecrets(rawProblemText).slice(0, 10000) : undefined;
  const language = rawLanguage ? rawLanguage.trim() : undefined;
  const errorText = rawError ? redactSecrets(rawError).slice(0, 2000) : undefined;
  const selection = selectionText ? redactSecrets(selectionText) : undefined;

  return {
    platform,
    url,
    title,
    problemText,
    language,
    errorText,
    selection,
  };
}
