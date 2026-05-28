// ─────────────────────────────────────────────────────────────────────────────
//  ava-interop.js
//  Namespace: AVA.UI (wwwroot static asset)
//  Purpose  : JS interop helpers called from Blazor components via IJSRuntime.
//             Served at: _content/AVA.UI/ava-interop.js
// ─────────────────────────────────────────────────────────────────────────────

window.avaScrollToBottom = (element) => {
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
};

window.avaScrollToBottomSmooth = (element) => {
    if (element) {
        element.scrollTo({ top: element.scrollHeight, behavior: 'smooth' });
    }
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
