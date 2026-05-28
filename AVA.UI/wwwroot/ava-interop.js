// ─────────────────────────────────────────────────────────────────────────────
//  ava-interop.js
//  Namespace: AVA.UI (wwwroot static asset)
//  Purpose  : JS interop helpers called from Blazor components via IJSRuntime.
//             Served at: _content/AVA.UI/ava-interop.js
// ─────────────────────────────────────────────────────────────────────────────

// Scroll a sentinel element into view — more reliable than scrollTop in Blazor.
window.avaScrollSentinelIntoView = (sentinel, smooth) => {
    if (!sentinel) return;
    sentinel.scrollIntoView({ block: 'end', behavior: smooth ? 'smooth' : 'instant' });
};

// Legacy — kept for MemoryLogPane.
window.avaScrollToBottom = (element) => {
    if (!element) return;
    element.scrollTop = element.scrollHeight;
};

window.avaScrollToBottomSmooth = (element) => {
    if (!element) return;
    element.scrollTo({ top: element.scrollHeight, behavior: 'smooth' });
};

window.avaIsAtBottom = (element, threshold) => {
    if (!element) return true;
    return element.scrollHeight - element.scrollTop - element.clientHeight <= threshold;
};

/**
 * Focus a DOM element by CSS selector.
 * Usage: await JS.InvokeVoidAsync("avaFocus", "#my-input");
 */
window.avaFocus = (selector) => {
    const el = document.querySelector(selector);
    if (el) el.focus();
};

/**
 * Copy text to clipboard.
 * Usage: await JS.InvokeAsync<bool>("avaCopyToClipboard", text);
 */
window.avaCopyToClipboard = async (text) => {
    try {
        await navigator.clipboard.writeText(text);
        return true;
    } catch {
        return false;
    }
};

/**
 * Get OS/platform info for adaptive UI hints.
 * Usage: var platform = await JS.InvokeAsync<string>("avaPlatform");
 */
window.avaPlatform = () => {
    return navigator.platform || navigator.userAgentData?.platform || 'unknown';
};

/**
 * Prevent default on a keydown event (e.g. Tab in textarea).
 * Registered as a non-passive event listener so preventDefault works.
 */
// ── Draggable floating panels ──────────────────────────────────────────────

window.avaDockMakeDraggable = function(PanelId, HeaderId, DotNetRef) {
    const Panel  = document.getElementById(PanelId);
    const Header = document.getElementById(HeaderId);
    if (!Panel || !Header) return;

    let IsDragging = false;
    let StartX = 0, StartY = 0;
    let PanelX = 0, PanelY = 0;

    Header.addEventListener('mousedown', function(E) {
        if (E.target.tagName === 'BUTTON') return;
        IsDragging = true;
        StartX = E.clientX;
        StartY = E.clientY;
        PanelX = Panel.offsetLeft;
        PanelY = Panel.offsetTop;
        E.preventDefault();
    });

    document.addEventListener('mousemove', function(E) {
        if (!IsDragging) return;
        Panel.style.left = (PanelX + E.clientX - StartX) + 'px';
        Panel.style.top  = (PanelY + E.clientY - StartY) + 'px';
    });

    document.addEventListener('mouseup', function() {
        if (!IsDragging) return;
        IsDragging = false;
        DotNetRef.invokeMethodAsync('OnDragEnd', Panel.offsetLeft, Panel.offsetTop);
    });
};

// ── Register textarea tab handler ──────────────────────────────────────────

window.avaRegisterTextareaTab = (element) => {
    if (!element) return;
    element.addEventListener('keydown', (e) => {
        if (e.key === 'Tab') {
            e.preventDefault();
            const start = element.selectionStart;
            const end = element.selectionEnd;
            element.value = element.value.substring(0, start) + '    ' + element.value.substring(end);
            element.selectionStart = element.selectionEnd = start + 4;
        }
    });
};

window.avaDownloadText = (fileName, content) => {
    const blob = new Blob([content ?? ''], { type: 'text/plain;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName;
    anchor.click();
    URL.revokeObjectURL(url);
};

window.avaDownloadPdfFromHtml = (title, html) => {
    const popup = window.open('', '_blank', 'noopener,noreferrer,width=1280,height=900');
    if (!popup) return;

    popup.document.open();
    popup.document.write(html);
    popup.document.close();
    popup.focus();
    popup.print();
};

const avaCanvasInterop = {
    root: null,
    dotNetRef: null,
    handlers: null,
    activeElement: null
};

const avaCanvasDispatchInput = (element) => {
    if (!element) return;
    element.dispatchEvent(new Event('input', { bubbles: true }));
    element.dispatchEvent(new Event('change', { bubbles: true }));
};

const avaCanvasReadSelection = (target, event) => {
    const block = target?.closest?.('[data-canvas-block-id]');
    const blockId = block?.dataset?.canvasBlockId ?? null;

    if (target && (target.tagName === 'TEXTAREA' || target.tagName === 'INPUT')) {
        avaCanvasInterop.activeElement = target;
        const start = target.selectionStart ?? 0;
        const end = target.selectionEnd ?? 0;
        const text = start !== end ? target.value.substring(start, end) : '';
        const rect = target.getBoundingClientRect();
        return {
            blockId,
            text,
            x: event?.clientX || rect.left + 24,
            y: event?.clientY || rect.top + 24
        };
    }

    const selection = window.getSelection();
    const text = selection ? selection.toString() : '';
    return {
        blockId,
        text,
        x: event?.clientX || 120,
        y: event?.clientY || 120
    };
};

window.avaCanvasInit = (root, dotNetRef) => {
    if (!root || !dotNetRef) return;

    window.avaCanvasDispose();

    avaCanvasInterop.root = root;
    avaCanvasInterop.dotNetRef = dotNetRef;

    const onMouseUp = (event) => {
        const data = avaCanvasReadSelection(event.target, event);
        dotNetRef.invokeMethodAsync('OnCanvasSelectionChanged', data.blockId, data.text, data.x, data.y);
    };

    const onKeyUp = (event) => {
        const data = avaCanvasReadSelection(event.target, event);
        dotNetRef.invokeMethodAsync('OnCanvasSelectionChanged', data.blockId, data.text, data.x, data.y);
    };

    const onContextMenu = (event) => {
        const editable = event.target?.closest?.('textarea,input,[contenteditable="true"]');
        if (!editable) return;

        event.preventDefault();
        const data = avaCanvasReadSelection(event.target, event);
        dotNetRef.invokeMethodAsync('OnCanvasContextMenuRequested', data.blockId, data.text, data.x, data.y);
    };

    const onDocumentPointerDown = (event) => {
        if (!root.contains(event.target)) {
            dotNetRef.invokeMethodAsync('OnCanvasDismissOverlays');
        }
    };

    root.addEventListener('mouseup', onMouseUp);
    root.addEventListener('keyup', onKeyUp);
    root.addEventListener('contextmenu', onContextMenu);
    document.addEventListener('pointerdown', onDocumentPointerDown);

    avaCanvasInterop.handlers = {
        onMouseUp,
        onKeyUp,
        onContextMenu,
        onDocumentPointerDown
    };
};

window.avaCanvasDispose = () => {
    const { root, handlers } = avaCanvasInterop;
    if (root && handlers) {
        root.removeEventListener('mouseup', handlers.onMouseUp);
        root.removeEventListener('keyup', handlers.onKeyUp);
        root.removeEventListener('contextmenu', handlers.onContextMenu);
        document.removeEventListener('pointerdown', handlers.onDocumentPointerDown);
    }

    avaCanvasInterop.root = null;
    avaCanvasInterop.dotNetRef = null;
    avaCanvasInterop.handlers = null;
    avaCanvasInterop.activeElement = null;
};

window.avaCanvasApplyInlineFormat = (format) => {
    const element = avaCanvasInterop.activeElement;
    if (!element || (element.tagName !== 'TEXTAREA' && element.tagName !== 'INPUT')) return;

    const start = element.selectionStart ?? 0;
    const end = element.selectionEnd ?? 0;
    if (start === end) return;

    const selectedText = element.value.substring(start, end);
    let replacement = selectedText;

    switch (format) {
        case 'bold':
            replacement = `**${selectedText}**`;
            break;
        case 'italic':
            replacement = `*${selectedText}*`;
            break;
        case 'code':
            replacement = `\`${selectedText}\``;
            break;
        default:
            break;
    }

    element.setRangeText(replacement, start, end, 'select');
    avaCanvasDispatchInput(element);
};

window.avaCanvasUndo = () => {
    document.execCommand('undo');
};

window.avaCanvasCopySelection = async () => {
    const element = avaCanvasInterop.activeElement;
    if (!element) return;
    const text = element.value.substring(element.selectionStart ?? 0, element.selectionEnd ?? 0);
    if (text) {
        await navigator.clipboard.writeText(text);
    }
};

window.avaCanvasCutSelection = async () => {
    const element = avaCanvasInterop.activeElement;
    if (!element) return;

    const start = element.selectionStart ?? 0;
    const end = element.selectionEnd ?? 0;
    const text = element.value.substring(start, end);
    if (text) {
        await navigator.clipboard.writeText(text);
        element.setRangeText('', start, end, 'start');
        avaCanvasDispatchInput(element);
    }
};

window.avaCanvasPasteSelection = async () => {
    const element = avaCanvasInterop.activeElement;
    if (!element) return;

    const pastedText = await navigator.clipboard.readText();
    const start = element.selectionStart ?? 0;
    const end = element.selectionEnd ?? 0;
    element.setRangeText(pastedText, start, end, 'end');
    avaCanvasDispatchInput(element);
};

window.avaCanvasSelectAll = () => {
    const element = avaCanvasInterop.activeElement;
    if (!element) return;
    element.focus();
    element.select();
};

window.avaCanvasInsertEmoji = () => {
    const element = avaCanvasInterop.activeElement;
    if (!element) return;

    const start = element.selectionStart ?? 0;
    const end = element.selectionEnd ?? 0;
    element.setRangeText('🙂', start, end, 'end');
    avaCanvasDispatchInput(element);
};

window.avaCanvasGetEditableText = (element) => {
    if (!element) return '';
    return (element.textContent || '').replace(/\u00A0/g, ' ').trimEnd();
};

window.avaCanvasSetEditableText = (element, value) => {
    if (!element) return;
    element.textContent = value || '';
};
