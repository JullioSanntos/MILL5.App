using CommunityToolkit.Mvvm.ComponentModel;
using MILL06.ViewModels.UIContracts;

namespace MILL06.ViewModels;

/// <summary>
/// Base ViewModel for content that participates in Region drag/drop behavior.
///
/// RegionBaseViewModel contains semantic Region behavior only. It has no
/// dependency on RegionCell, Draggable, DropTarget, or other View controls.
/// </summary>
public abstract partial class RegionBaseViewModel : BaseViewModel {

    #region Drag Drop Responsibility Reference
    /*
    RegionBaseViewModel — content-owned extensibility
        GetCanBeDragged(RegionNode)          Per-presentation source capability; the same ViewModel may be shown in multiple Regions.
        GetCanBeReplaced(RegionNode)         Per-presentation target capability; does not decide compatibility with a specific incoming ViewModel.
        DragDropCapabilitiesChanged          Invalidates calculated RegionNode capabilities when business state changes.
        RaiseDragDropCapabilitiesChanged()   Protected trigger used by derived ViewModels when capability answers may have changed.

    RegionNode — Region state + calculated capabilities
        CanBeDragged                         Calculated from the payload ViewModel for this specific Region presentation.
        CanBeReplaced                        Calculated from the payload ViewModel; empty Regions are replaceable by default.
        RegionAssigning                      Cancellable final gate with source, target, and incoming ViewModel all available.
        RegionAssigned                       Completion notification raised only after a successful assignment.

    MainViewModel — transaction/orchestration
        Interpret DragData                   Converts generic drag data into the semantic source being assigned or moved.
        Resolve incoming ViewModel           Converts descriptors such as MenuItemViewModel into actual Region content.
        Validate source/target               Enforces capabilities and raises RegionAssigning before mutating Region state.
        Perform assignment/move              Mutates RegionNode state, updates ActiveRegionNode, then raises RegionAssigned.

    Draggable / DropTarget — physical View behavior
        Pointer / gesture / cursor           Own platform interaction only; no Region or business semantics.
        DragData                             Carries the source descriptor unchanged to the drop destination.
        Visual cues                          Own hover, dragging, available-target, drag-over, and dropped appearance.
    */

    #endregion Drag Drop Responsibility Reference

    #region Active Region

    /// <summary>
    /// Gets or sets the Region presentation of this ViewModel that is
    /// currently being operated on.
    ///
    /// A ViewModel may be presented by more than one RegionNode.
    /// ActiveRegionNode identifies the currently active presentation.
    ///
    /// This is application state and is not required to identify the
    /// source of a drag operation; a Region drag carries its RegionNode.
    /// </summary>
    [ObservableProperty]
    private RegionNode? _activeRegionNode;

    #endregion Active Region

    #region Drag Drop Capabilities

    /// <summary>
    /// Determines whether this ViewModel may be dragged from the specified
    /// Region.
    ///
    /// Derived ViewModels may consider business state, Region location,
    /// Region-tree state, or other semantic conditions.
    /// </summary>
    public virtual bool GetCanBeDragged(RegionNode regionNode) {
        return true;
    }

    /// <summary>
    /// Determines whether this ViewModel may be replaced in the specified
    /// Region.
    ///
    /// Derived ViewModels may consider business state, Region location,
    /// Region-tree state, or other semantic conditions.
    /// </summary>
    public virtual bool GetCanBeReplaced(RegionNode regionNode) {
        return true;
    }

    /// <summary>
    /// Raised when business state changes in a way that may change the
    /// answers returned by GetCanBeDragged or GetCanBeReplaced.
    ///
    /// RegionNodes presenting this ViewModel use this notification to
    /// reevaluate their calculated drag/drop capabilities.
    /// </summary>
    public event EventHandler? DragDropCapabilitiesChanged;

    /// <summary>
    /// Notifies RegionNodes presenting this ViewModel that their drag/drop
    /// capabilities should be reevaluated.
    /// </summary>
    protected void RaiseDragDropCapabilitiesChanged() {
        DragDropCapabilitiesChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion Drag Drop Capabilities
}