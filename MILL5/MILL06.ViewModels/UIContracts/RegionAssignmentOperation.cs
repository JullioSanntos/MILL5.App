namespace MILL06.ViewModels.UIContracts;

/// <summary>
/// Describes what happens to the incoming ViewModel during assignment.
/// </summary>
public enum RegionAssignmentOperation {

    /// <summary>
    /// Assigns an independently obtained ViewModel to the target.
    /// The source region, if supplied for context, is not modified.
    ///
    /// Example:
    /// Menu double-click or dragging a Menu tree item.
    /// </summary>
    Assign,

    /// <summary>
    /// Assigns a copy/independent representation to the target.
    /// The source remains unchanged.
    ///
    /// Reserved for scenarios where copy semantics are required.
    /// </summary>
    Copy,

    /// <summary>
    /// Moves an existing ViewModel from the source region to the target.
    /// The source participates in the RegionRemoving lifecycle.
    /// </summary>
    Move
}