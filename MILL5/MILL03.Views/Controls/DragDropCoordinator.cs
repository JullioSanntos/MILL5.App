namespace MILL03.Views.Controls;

public static class DragDropCoordinator {
    public static event EventHandler? DragStarted;
    public static event EventHandler? DragEnded;

    public static object? DragData { get; private set; }

    public static bool IsDragging => DragData != null;

    public static void Begin(object dragData) {
        DragData = dragData;
        DragStarted?.Invoke(null, EventArgs.Empty);
    }

    public static void End() {
        if (DragData == null) return;

        DragData = null;
        DragEnded?.Invoke(null, EventArgs.Empty);
    }
}