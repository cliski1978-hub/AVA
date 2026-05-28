using AVA.UI.CORE.Models.UI;
using AVA.UI.State;

namespace AVA.UI.Features.Canvas.ViewModels;

/// <summary>
/// ViewModel for SessionCanvas.razor.
/// Owns all drag and resize transient state. Calls SaveCanvasState on release.
/// Per-instance — one created per SessionCanvas component.
/// </summary>
public class SessionCanvasVM : IDisposable
{
    private readonly AppState _appState;

    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    // ── Drag state ────────────────────────────────────────────────────────────
    public CardState? Dragging { get; private set; }
    public string? DraggingCardId { get; private set; }
    public double DragOffsetX { get; private set; }
    public double DragOffsetY { get; private set; }

    // ── Resize state ──────────────────────────────────────────────────────────
    public CardState? Resizing { get; private set; }
    public string ResizeDir { get; private set; } = string.Empty;

    private double _resizeStartMouseX;
    private double _resizeStartMouseY;
    private double _resizeStartCardX;
    private double _resizeStartCardY;
    private double _resizeStartWidth;
    private double _resizeStartHeight;

    private const double MinWidth  = 280;
    private const double MinHeight = 200;

    public string CaptureCursor => ResizeDir switch
    {
        "n" or "s"   => "ns-resize",
        "e" or "w"   => "ew-resize",
        "ne" or "sw" => "nesw-resize",
        "nw" or "se" => "nwse-resize",
        _            => "grabbing"
    };

    public SessionCanvasVM(AppState appState)
    {
        _appState = appState;
    }

    // ── Activate ──────────────────────────────────────────────────────────────
    public void Activate(CardState card)
    {
        var cards = _appState.CurrentSession?.Canvas.Cards;
        if (cards == null || !cards.Any()) return;
        card.ZIndex = cards.Max(c => c.ZIndex) + 1;
        Notify();
    }

    // ── Drag ──────────────────────────────────────────────────────────────────
    public void StartDrag((CardState Card, double OffsetX, double OffsetY) args)
    {
        Dragging       = args.Card;
        DraggingCardId = args.Card.CardId;
        DragOffsetX    = args.OffsetX;
        DragOffsetY    = args.OffsetY;
        Resizing       = null;
    }

    // ── Resize ────────────────────────────────────────────────────────────────
    public void StartResize((CardState Card, string Direction, double MouseX, double MouseY) args)
    {
        Resizing            = args.Card;
        ResizeDir           = args.Direction;
        _resizeStartMouseX  = args.MouseX;
        _resizeStartMouseY  = args.MouseY;
        _resizeStartCardX   = args.Card.X;
        _resizeStartCardY   = args.Card.Y;
        _resizeStartWidth   = args.Card.Width;
        _resizeStartHeight  = args.Card.Height ?? 400;
        Dragging            = null;
        args.Card.Height    = _resizeStartHeight;

        var cards = _appState.CurrentSession?.Canvas.Cards;
        if (cards != null && cards.Any())
            Resizing.ZIndex = cards.Max(c => c.ZIndex) + 1;
    }

    // ── Capture move ──────────────────────────────────────────────────────────
    public void OnCaptureMove(double clientX, double clientY)
    {
        if (Dragging != null)
        {
            Dragging.X = Math.Max(0, clientX - DragOffsetX);
            Dragging.Y = Math.Max(0, clientY - DragOffsetY);
            Notify();
            return;
        }

        if (Resizing != null)
        {
            var dx = clientX - _resizeStartMouseX;
            var dy = clientY - _resizeStartMouseY;

            var newX = _resizeStartCardX;
            var newY = _resizeStartCardY;
            var newW = _resizeStartWidth;
            var newH = _resizeStartHeight;

            if (ResizeDir.Contains('e')) newW = Math.Max(MinWidth, _resizeStartWidth + dx);
            if (ResizeDir.Contains('w')) { newW = Math.Max(MinWidth, _resizeStartWidth - dx); newX = _resizeStartCardX + (_resizeStartWidth - newW); }
            if (ResizeDir.Contains('s')) newH = Math.Max(MinHeight, _resizeStartHeight + dy);
            if (ResizeDir.Contains('n')) { newH = Math.Max(MinHeight, _resizeStartHeight - dy); newY = _resizeStartCardY + (_resizeStartHeight - newH); }

            Resizing.X      = Math.Max(0, newX);
            Resizing.Y      = Math.Max(0, newY);
            Resizing.Width  = newW;
            Resizing.Height = newH;
            Notify();
        }
    }

    // ── Capture up ────────────────────────────────────────────────────────────
    public void OnCaptureUp()
    {
        if (Dragging == null && Resizing == null) return;
        Dragging       = null;
        DraggingCardId = null;
        Resizing       = null;
        ResizeDir      = string.Empty;
        _appState.SaveCanvasState();
        Notify();
    }

    public void Dispose() { }
}
