using MILL06.ViewModels.UIContracts;

namespace MILL06.ViewModels;

/// <summary>
/// Base ViewModel for content that participates in Region drag/drop behavior.
///
/// RegionBaseViewModel contains semantic Region behavior only. It has no
/// dependency on RegionCell, Draggable, DropTarget, or other View controls.
/// </summary>
public abstract partial class RegionBaseViewModel : BaseViewModel {

    #region Drag Drop Capabilities

    /// <summary>
    /// Determines whether this ViewModel may currently participate as
    /// the ViewModel being dragged from its Region.
    /// </summary>
    public virtual bool CanBeDragged => true;

    /// <summary>
    /// Determines whether the Region presenting this ViewModel may
    /// generally be replaced by another ViewModel.
    ///
    /// More specific assignment policy belongs in OnRegionAssigning.
    /// </summary>
    public virtual bool CanBeReplaced => true;

    /// <summary>
    /// Raised when business state changes in a way that may change
    /// CanBeDragged or CanBeReplaced.
    /// </summary>
    public event EventHandler? DragDropCapabilitiesChanged;

    /// <summary>
    /// Notifies RegionNodes presenting this ViewModel that their
    /// drag/drop capabilities should be reevaluated.
    /// </summary>
    protected void RaiseDragDropCapabilitiesChanged() {
        DragDropCapabilitiesChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion Drag Drop Capabilities

    #region Region Assignment Lifecycle

    /// <summary>
    /// Optional externally supplied assignment policy.
    ///
    /// null  = no opinion; use this ViewModel's normal lifecycle policy.
    /// true  = explicitly allow this participant's involvement.
    /// false = cancel the assignment.
    /// </summary>
    public Func<RegionAssigningContext, bool?>? RegionAssigningCallback { get; set; }

    /// <summary>
    /// Called before a Region drag/drop assignment is committed.
    ///
    /// Infrastructure calls each distinct participating ViewModel once,
    /// in this order: target, source when different from the dragged
    /// ViewModel, and dragged ViewModel last.
    ///
    /// Derived ViewModels may call base to retain the default policy or
    /// omit the base call to replace that policy completely.
    /// </summary>
    protected internal virtual void OnRegionAssigning(RegionAssigningContext context) {
        var callbackResult = RegionAssigningCallback?.Invoke(context);

        if (callbackResult.HasValue) {
            context.Cancel = !callbackResult.Value;
            return;
        }

        var isTarget =
            ReferenceEquals(context.TargetNode.PayloadViewModel, this);

        var isSource =
            ReferenceEquals(context.SourceNode.PayloadViewModel, this);

        var isDragged =
            ReferenceEquals(context.IncomingViewModel, this);

        // A target normally obeys its general replaceability rule.
        if (isTarget && !CanBeReplaced) {
            context.Cancel = true;
            return;
        }

        if (!isSource) return;

        // Normal case: the Region is transporting its own payload.
        if (isDragged) {
            context.Cancel = !CanBeDragged;
            return;
        }

        // Exceptional case: this Region is transporting another ViewModel.
        // Derived ViewModels must explicitly authorize that behavior.
        context.Cancel = true;
    }

    #endregion Region Assignment Lifecycle
}