namespace MILL06.ViewModels.UIContracts;

/// <summary>
/// Identifies how a region assignment was initiated.
/// This describes the UI/application origin, not what the assignment does.
/// </summary>
public enum RegionAssignmentOrigin {
    Startup,
    DoubleClick,
    DragDrop,
    Programmatic
}