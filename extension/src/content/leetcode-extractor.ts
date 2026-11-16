import { ProblemContextPayload } from '../shared/extension-messaging';

export interface ExtractedProblem extends ProblemContextPayload {
  platform: 1;
}

/**
 * Extracts problem slug from LeetCode URL patterns:
 * e.g. https://leetcode.com/problems/two-sum/
 *      https://leetcode.com/problems/two-sum/description/
 *      https://leetcode.cn/problems/two-sum/
 */
export function extractSlugFromUrl(url: string): string | null {
  try {
    const parsed = new URL(url);
    if (!parsed.hostname.includes('leetcode.')) {
      return null;
    }
    const match = parsed.pathname.match(/\/problems\/([^/]+)/);
    return match ? match[1] : null;
  } catch {
    return null;
  }
}

/**
 * Strips script tags, buttons, SVGs, and extraneous UI chrome from cloned container.
 * Returns clean plain text / formatted text for description.
 */
export function cleanDescription(container: Element): string {
  const clone = container.cloneNode(true) as Element;

  // Remove interactive and UI chrome elements
  const removeSelectors = [
    'script',
    'style',
    'button',
    'svg',
    'nav',
    'header',
    'footer',
    '[role="button"]',
    '.shrink-0',
    '.copy-button',
    'button[class*="copy"]',
    'div[class*="tools"]',
  ];

  removeSelectors.forEach(selector => {
    clone.querySelectorAll(selector).forEach(el => el.remove());
  });

  // Extract clean text, preserving line breaks for paragraphs, list items, and code blocks
  const text = clone.textContent || '';
  
  // Normalize whitespace: collapse multiple blank lines to at most two
  return text
    .replace(/\r\n/g, '\n')
    .replace(/[ \t]+/g, ' ')
    .replace(/\n{3,}/g, '\n\n')
    .trim();
}

/**
 * Resiliently extracts problem details from LeetCode DOM.
 */
export function extractProblemFromDocument(
  doc: Document,
  currentUrl: string
): ExtractedProblem | null {
  const slug = extractSlugFromUrl(currentUrl);
  if (!slug) {
    return null;
  }

  // 1. Title Extraction
  const titleSelectors = [
    'div[data-cy="question-title"]',
    'div.text-title-large',
    'a[href*="/problems/' + slug + '"]',
    'h4[data-cy="question-title"]',
    'div[class*="text-title-"]',
    'h1',
  ];

  let rawTitle = '';
  for (const selector of titleSelectors) {
    const el = doc.querySelector(selector);
    if (el && el.textContent && el.textContent.trim().length > 0) {
      rawTitle = el.textContent.trim();
      break;
    }
  }

  // Fallback to title from document.title if not found
  if (!rawTitle && doc.title) {
    // LeetCode document titles typically look like "1. Two Sum - LeetCode"
    const titleMatch = doc.title.match(/^([^–-]+)/);
    if (titleMatch) {
      rawTitle = titleMatch[1].trim();
    }
  }

  if (!rawTitle) {
    // If still empty, format the slug nicely as a last resort
    rawTitle = slug
      .split('-')
      .map(word => word.charAt(0).toUpperCase() + word.slice(1))
      .join(' ');
  }

  // Parse Problem ID and Title
  // e.g. "1. Two Sum" -> id: "1", title: "Two Sum"
  let problemId = slug;
  let title = rawTitle;
  const idPrefixMatch = rawTitle.match(/^(\d+)\.\s*(.+)$/);
  if (idPrefixMatch) {
    problemId = idPrefixMatch[1].trim();
    title = idPrefixMatch[2].trim();
  }

  // 2. Difficulty Extraction
  let difficulty = 'Medium';
  const difficultySelectors = [
    'div[class*="text-difficulty-"]',
    'div.text-olive', // LeetCode green (Easy)
    'div.text-yellow', // LeetCode yellow (Medium)
    'div.text-pink', // LeetCode red/pink (Hard)
    'span[class*="text-difficulty-"]',
    'div[data-degree]',
  ];

  for (const selector of difficultySelectors) {
    const el = doc.querySelector(selector);
    const text = el?.textContent?.trim().toLowerCase();
    if (text === 'easy') {
      difficulty = 'Easy';
      break;
    } else if (text === 'medium') {
      difficulty = 'Medium';
      break;
    } else if (text === 'hard') {
      difficulty = 'Hard';
      break;
    }
  }

  if (difficulty === 'Medium') {
    // Direct text search if selectors didn't match
    const candidates = Array.from(doc.querySelectorAll('div, span'));
    for (const el of candidates) {
      // Look at small badges
      if (el.children.length === 0) {
        const text = el.textContent?.trim() || '';
        if (/^Easy$/i.test(text)) {
          difficulty = 'Easy';
          break;
        } else if (/^Medium$/i.test(text)) {
          difficulty = 'Medium';
          break;
        } else if (/^Hard$/i.test(text)) {
          difficulty = 'Hard';
          break;
        }
      }
    }
  }

  // 3. Description Extraction
  const descriptionSelectors = [
    'div[data-track-load="description_content"]',
    'div[data-key="description-content"]',
    'div[class*="elfjS"]',
    'div._1l12s',
    'div.question-content',
    'div[class*="content__"]',
  ];

  let description = '';
  for (const selector of descriptionSelectors) {
    const el = doc.querySelector(selector);
    if (el) {
      const cleaned = cleanDescription(el);
      if (cleaned.length > 20) {
        description = cleaned;
        break;
      }
    }
  }

  // If no description container was found, try fallback article or question area
  if (!description) {
    const fallbackEl = doc.querySelector('article, [role="main"]');
    if (fallbackEl) {
      description = cleanDescription(fallbackEl);
    }
  }

  // Bound description length per Codexo API constraints (max 20,000 characters)
  if (description.length > 20000) {
    description = description.substring(0, 20000);
  }

  if (!description) {
    return null;
  }

  // Construct normalized URL
  const normalizedUrl = `https://leetcode.com/problems/${slug}/`;

  return {
    platform: 1,
    platformProblemId: problemId,
    title,
    slug,
    url: normalizedUrl,
    difficulty,
    description,
  };
}
