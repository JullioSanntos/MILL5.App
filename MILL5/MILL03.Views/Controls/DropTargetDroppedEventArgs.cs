namespace MILL03.Views.Controls;

public sealed class DropTargetDroppedEventArgs(
    object dragData,
    DragDropOperation operation) : EventArgs {

    public object DragData { get; } = dragData;
    public DragDropOperation Operation { get; } = operation;
}