// Codexo MAIN-World Editor Reader
// Reads full source code directly from editor instances in window context
// (Monaco, CodeMirror 6, CodeMirror 5, Ace, or Textarea).
// Avoids scraping visible DOM lines because modern editors virtualize lines.

export interface EditorReadResult {
  code: string | null;
  editor: 'monaco' | 'codemirror6' | 'codemirror5' | 'ace' | 'textarea' | null;
  lineCount?: number;
  language?: string;
}

export function extractCodeFromMainWorld(win: any = typeof window !== 'undefined' ? window : {}): EditorReadResult {
  const doc: Document | undefined = win.document;
  if (!doc) {
    return { code: null, editor: null };
  }

  // 1. Monaco Editor (LeetCode, VS Code web)
  try {
    if (win.monaco?.editor?.getModels) {
      const models = win.monaco.editor.getModels();
      if (models && models.length > 0) {
        const validModel = models.find((m: any) => m.getValue && m.getValue().trim().length > 0) || models[0];
        if (validModel?.getValue) {
          const code = validModel.getValue();
          const lineCount = validModel.getLineCount ? validModel.getLineCount() : code.split('\n').length;
          const language = validModel.getLanguageId ? validModel.getLanguageId() : undefined;
          return {
            code,
            editor: 'monaco',
            lineCount,
            language,
          };
        }
      }
    }
  } catch {
    // Fall through
  }

  // 2. CodeMirror 6 (modern web apps, newer LeetCode containers)
  try {
    const cm6Content = doc.querySelector('.cm-content');
    if (cm6Content) {
      const cmView = (cm6Content as any).cmView;
      if (cmView?.view?.state?.doc) {
        const code = cmView.view.state.doc.toString();
        const lineCount = cmView.view.state.doc.lines || code.split('\n').length;
        return {
          code,
          editor: 'codemirror6',
          lineCount,
        };
      }
    }
  } catch {
    // Fall through
  }

  // 3. CodeMirror 5 (Codeforces, HackerRank, GeeksforGeeks)
  try {
    const cm5El = doc.querySelector('.CodeMirror');
    if (cm5El && (cm5El as any).CodeMirror?.getValue) {
      const cmInstance = (cm5El as any).CodeMirror;
      const code = cmInstance.getValue();
      const lineCount = cmInstance.lineCount ? cmInstance.lineCount() : code.split('\n').length;
      return {
        code,
        editor: 'codemirror5',
        lineCount,
      };
    }
  } catch {
    // Fall through
  }

  // 4. Ace Editor (HackerRank, CS Academy)
  try {
    const aceEl = doc.querySelector('.ace_editor');
    if (aceEl && win.ace?.edit) {
      const editorInstance = win.ace.edit(aceEl);
      if (editorInstance?.getValue) {
        const code = editorInstance.getValue();
        const lineCount = editorInstance.session?.getLength ? editorInstance.session.getLength() : code.split('\n').length;
        return {
          code,
          editor: 'ace',
          lineCount,
        };
      }
    }
  } catch {
    // Fall through
  }

  // 5. Standard Textarea Fallback
  try {
    const textarea = doc.querySelector<HTMLTextAreaElement>(
      'textarea.code, textarea#code, textarea[name="source"], textarea[name="code"], textarea.editor'
    );
    if (textarea && textarea.value) {
      const code = textarea.value;
      return {
        code,
        editor: 'textarea',
        lineCount: code.split('\n').length,
      };
    }
  } catch {
    // Fall through
  }

  // 6. No editor found
  return {
    code: null,
    editor: null,
  };
}
