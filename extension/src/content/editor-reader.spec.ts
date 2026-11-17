import { describe, it, expect, beforeEach } from 'vitest';
import { extractCodeFromMainWorld } from './editor-reader';

describe('EditorReader (MAIN World)', () => {
  let mockWindow: any;
  let mockDoc: Document;

  beforeEach(() => {
    mockDoc = document.implementation.createHTMLDocument();
    mockWindow = {
      document: mockDoc,
    };
  });

  it('should return null when no editor is present on the page', () => {
    const result = extractCodeFromMainWorld(mockWindow);
    expect(result.code).toBeNull();
    expect(result.editor).toBeNull();
  });

  it('should extract full code from Monaco editor models', () => {
    const monacoCode = 'def two_sum(nums, target):\n    lookup = {}\n    for i, num in enumerate(nums):\n        if target - num in lookup:\n            return [lookup[target - num], i]\n        lookup[num] = i';
    mockWindow.monaco = {
      editor: {
        getModels: () => [
          {
            getValue: () => monacoCode,
            getLineCount: () => 6,
            getLanguageId: () => 'python',
          },
        ],
      },
    };

    const result = extractCodeFromMainWorld(mockWindow);
    expect(result.editor).toBe('monaco');
    expect(result.code).toBe(monacoCode);
    expect(result.lineCount).toBe(6);
    expect(result.language).toBe('python');
  });

  it('should extract full code from CodeMirror 6 view state', () => {
    const cm6Div = mockDoc.createElement('div');
    cm6Div.className = 'cm-content';
    const cm6Code = 'public class Solution { public int TwoSum() { return 0; } }';
    (cm6Div as any).cmView = {
      view: {
        state: {
          doc: {
            toString: () => cm6Code,
            lines: 1,
          },
        },
      },
    };
    mockDoc.body.appendChild(cm6Div);

    const result = extractCodeFromMainWorld(mockWindow);
    expect(result.editor).toBe('codemirror6');
    expect(result.code).toBe(cm6Code);
    expect(result.lineCount).toBe(1);
  });

  it('should extract full code from CodeMirror 5 instance', () => {
    const cm5Div = mockDoc.createElement('div');
    cm5Div.className = 'CodeMirror';
    const cm5Code = '#include <iostream>\nusing namespace std;\nint main() { return 0; }';
    (cm5Div as any).CodeMirror = {
      getValue: () => cm5Code,
      lineCount: () => 3,
    };
    mockDoc.body.appendChild(cm5Div);

    const result = extractCodeFromMainWorld(mockWindow);
    expect(result.editor).toBe('codemirror5');
    expect(result.code).toBe(cm5Code);
    expect(result.lineCount).toBe(3);
  });

  it('should extract code from Ace editor instance', () => {
    const aceDiv = mockDoc.createElement('div');
    aceDiv.className = 'ace_editor';
    mockDoc.body.appendChild(aceDiv);

    const aceCode = 'fn main() {\n    println!("Hello, World!");\n}';
    mockWindow.ace = {
      edit: (el: any) => {
        if (el === aceDiv) {
          return {
            getValue: () => aceCode,
            session: { getLength: () => 3 },
          };
        }
        return null;
      },
    };

    const result = extractCodeFromMainWorld(mockWindow);
    expect(result.editor).toBe('ace');
    expect(result.code).toBe(aceCode);
    expect(result.lineCount).toBe(3);
  });

  it('should extract code from fallback textarea element', () => {
    const textarea = mockDoc.createElement('textarea');
    textarea.className = 'code';
    textarea.value = 'SELECT * FROM users WHERE active = 1;';
    mockDoc.body.appendChild(textarea);

    const result = extractCodeFromMainWorld(mockWindow);
    expect(result.editor).toBe('textarea');
    expect(result.code).toBe('SELECT * FROM users WHERE active = 1;');
    expect(result.lineCount).toBe(1);
  });
});
