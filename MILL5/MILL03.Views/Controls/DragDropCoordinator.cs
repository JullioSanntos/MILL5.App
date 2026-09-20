namespace MILL03.Views.Controls;

public static class DragDropCoordinator {
    public static object? DragData { get; private set; }

    public static bool IsDragging => DragData != null;

    public static void Begin(object dragData) {
        DragData = dragData;
    }

    public static void End() {
        DragData = null;
    }
}