using MILL06.ViewModels;

namespace MILL06.ViewModels.UIContracts;

/// <summary>
/// Describes one proposed or executing assignment of a ViewModel to a Region.
///
/// The same context travels through:
///     - eligibility evaluation,
///     - cancellable pre-change lifecycle methods,
///     - mutation,
///     - post-change lifecycle methods.
///
/// Views are intentionally excluded. The RegionNodes provide access to their
/// current ViewModels while View resolution remains a View-layer concern.
/// </summary>
public sealed class RegionAssignmentContext(
    RegionNode rootRegionNode,
    RegionNode? sourceRegionNode,
    RegionNode targetRegionNode,
    RegionBaseViewModel incomingViewModel,
    RegionAssignmentOrigin origin,
    RegionAssignmentOperation operation) {
    #region Properties

    /// <summary>
    /// Root of the complete RegionNode tree.
    ///
    /// Allows participating ViewModels to inspect the surrounding Region
    /// structure when making contextual decisions.
    /// </summary>
    public RegionNode RootRegionNode { get; } = rootRegionNode ??
                                                throw new ArgumentNullException(nameof(rootRegionNode));

    /// <summary>
    /// Region from which the operation originated.
    ///
    /// This does not imply removal. The source is modified only for a Move.
    ///
    /// Examples:
    ///     DragDrop Move  -> Region being dragged.
    ///     DoubleClick    -> Region containing the Menu.
    ///     Startup        -> Usually null.
    /// </summary>
    public RegionNode? SourceRegionNode { get; } = sourceRegionNode;

    /// <summary>
    /// Region proposed to receive IncomingViewModel.
    /// </summary>
    public RegionNode TargetRegionNode { get; } = targetRegionNode ??
                                                  throw new ArgumentNullException(nameof(targetRegionNode));

    /// <summary>
    /// ViewModel proposed for assignment to TargetRegionNode.
    ///
    /// It cannot always be obtained from SourceRegionNode. A Menu double-click,
    /// for example, originates in MenuViewModel but may create a completely
    /// different ViewModel for assignment.
    /// </summary>
    public RegionBaseViewModel IncomingViewModel { get; } = incomingViewModel ??
                                                            throw new ArgumentNullException(nameof(incomingViewModel));

    /// <summary>
    /// Describes how the assignment originated.
    /// </summary>
    public RegionAssignmentOrigin Origin { get; } = origin;

    /// <summary>
    /// Describes whether the incoming ViewModel is being independently
    /// assigned, copied, or moved from another Region.
    /// </summary>
    public RegionAssignmentOperation Operation { get; } = operation;

    #endregion Properties

    #region Constructor

    #endregion Constructor
}