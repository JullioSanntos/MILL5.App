using System.ComponentModel;
using MILL06.ViewModels;

namespace MILL06.ViewModels.UIContracts;

/// <summary>
/// Provides the complete context for a pending Region drag/drop assignment.
///
/// SourceNode identifies the Region where the drag originated.
/// DraggedViewModel identifies the ViewModel actually being transported and
/// may differ from SourceNode.PayloadViewModel.
/// TargetNode identifies the destination Region and may have an empty payload.
///
/// Either participating ViewModel may cancel the operation.
/// </summary>
public sealed class RegionAssigningContext(
    RegionNode sourceNode,
    BaseViewModel draggedViewModel,
    RegionNode targetNode)
    : CancelEventArgs {

    /// <summary>
    /// Region where the drag originated. Its PayloadViewModel identifies
    /// the source participant.
    /// </summary>
    public RegionNode SourceNode { get; } =
        sourceNode ?? throw new ArgumentNullException(nameof(sourceNode));

    /// <summary>
    /// ViewModel being transported by the drag operation. This is normally
    /// SourceNode.PayloadViewModel, but may differ, as with MenuViewModel.
    /// </summary>
    public BaseViewModel DraggedViewModel { get; } =
        draggedViewModel ?? throw new ArgumentNullException(nameof(draggedViewModel));

    /// <summary>
    /// Destination Region. Its PayloadViewModel identifies the target
    /// participant and may be null.
    /// </summary>
    public RegionNode TargetNode { get; } =
        targetNode ?? throw new ArgumentNullException(nameof(targetNode));
}