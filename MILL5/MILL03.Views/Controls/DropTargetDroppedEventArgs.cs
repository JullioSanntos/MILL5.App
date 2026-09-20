namespace MILL03.Views.Controls;

public sealed class DropTargetDroppedEventArgs(object dragData) : EventArgs {
    public object DragData { get; } = dragData;
}