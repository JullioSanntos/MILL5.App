using CommunityToolkit.Mvvm.ComponentModel;
using MILL06.ViewModels.UIContracts;

namespace MILL06.ViewModels;

/// <summary>
/// Base ViewModel for content that participates in the Region lifecycle.
///
/// RegionViewModel contains semantic Region behavior only. It has no
/// dependency on RegionCell, Draggable, DropTarget, Views, or other
/// MAUI visual controls.
/// </summary>
public abstract partial class RegionBaseViewModel : BaseViewModel {
    #region Interaction Defaults

    /// <summary>
    /// Indicates whether this ViewModel may normally be moved from the Region
    /// in which it is currently presented to another Region.
    ///
    /// Movable is intentionally different from draggable. A draggable object,
    /// such as a MenuNodeViewModel leaf, may act only as the source of a drag
    /// operation that requests creation or assignment of Region content.
    ///
    /// A movable RegionBaseViewModel is already assigned to a Region and may
    /// itself be relocated from that source Region to another Region.
    ///
    /// The RegionCell may independently restrict moving at the layout level.
    /// Context-specific exceptions are evaluated through CanRemoveFromRegion.
    /// </summary>
    [ObservableProperty]
    private bool _isMovable = true;

    /// <summary>
    /// Indicates whether this ViewModel normally permits another ViewModel
    /// to replace it in its current Region.
    ///
    /// This is a default. CanBeReplacedBy may make a contextual exception.
    ///
    /// Example:
    /// a protected Menu may normally be non-replaceable, while still allowing
    /// another MenuViewModel to replace it.
    /// </summary>
    [ObservableProperty]
    private bool _isReplaceable = true;

    #endregion Interaction Defaults

    #region Pure Decisions

    /// <summary>
    /// Determines whether this ViewModel, currently occupying the target
    /// Region, may be replaced by the proposed incoming ViewModel.
    ///
    /// Called repeatedly while evaluating possible targets, including
    /// drag-over visualization. Implementations should therefore have no
    /// side effects.
    ///
    /// currentDecision contains the decision already produced by the
    /// RegionCell's layout policy and this ViewModel's default policy.
    /// </summary>
    public bool CanBeReplacedBy(
        RegionAssignmentContext context,
        bool currentDecision) {

        return OnCanBeReplacedBy(context, currentDecision);
    }

    protected virtual bool OnCanBeReplacedBy(
        RegionAssignmentContext context,
        bool currentDecision) {

        return currentDecision;
    }

    /// <summary>
    /// Determines whether this ViewModel may leave its current Region
    /// during a Move operation.
    ///
    /// Called during preview/validation and must not produce side effects.
    /// </summary>
    public bool CanRemoveFromRegion(
        RegionAssignmentContext context,
        bool currentDecision) {

        return OnCanRemoveFromRegion(context, currentDecision);
    }

    protected virtual bool OnCanRemoveFromRegion(
        RegionAssignmentContext context,
        bool currentDecision) {

        return currentDecision;
    }

    /// <summary>
    /// Determines whether this incoming ViewModel may occupy the proposed
    /// target Region.
    ///
    /// Called during preview/validation and must not produce side effects.
    /// </summary>
    public bool CanAssignToRegion(
        RegionAssignmentContext context,
        bool currentDecision) {

        return OnCanAssignToRegion(context, currentDecision);
    }

    protected virtual bool OnCanAssignToRegion(
        RegionAssignmentContext context,
        bool currentDecision) {

        return currentDecision;
    }

    #endregion Pure Decisions

    #region Replacement Lifecycle

    /// <summary>
    /// Invoked on the ViewModel currently occupying the target Region.
    ///
    /// Lifecycle position:
    ///     validation accepted
    ///         ↓
    ///     RegionReplacing
    ///         ↓
    ///     Region mutation
    ///
    /// Returning false cancels the complete assignment transaction.
    /// </summary>
    public bool RaiseRegionReplacing(
        RegionAssignmentContext context,
        bool currentDecision) {

        if (!CanBeReplacedBy(context, currentDecision)) return false;

        var e = new RegionAssignmentChangingEventArgs(context);
        OnRegionReplacing(e);

        return !e.Cancel;
    }

    /// <summary>
    /// Last cancellable notification before this ViewModel is replaced.
    ///
    /// Contextual acceptance belongs in OnCanBeReplacedBy. This lifecycle
    /// point is intended for final validation or preparation.
    /// </summary>
    protected virtual void OnRegionReplacing(
        RegionAssignmentChangingEventArgs e) {
    }

    /// <summary>
    /// Invoked after this ViewModel has been replaced.
    /// </summary>
    public void RaiseRegionReplaced(
        RegionAssignmentContext context) {

        OnRegionReplaced(context);
    }

    protected virtual void OnRegionReplaced(
        RegionAssignmentContext context) {
    }

    #endregion Replacement Lifecycle

    #region Removal Lifecycle

    /// <summary>
    /// Invoked on the source ViewModel before it leaves its Region.
    ///
    /// This lifecycle participates only in Move operations.
    /// Returning false cancels the complete transaction.
    /// </summary>
    public bool RaiseRegionRemoving(
        RegionAssignmentContext context,
        bool currentDecision) {

        if (!CanRemoveFromRegion(context, currentDecision)) return false;

        var e = new RegionAssignmentChangingEventArgs(context);
        OnRegionRemoving(e);

        return !e.Cancel;
    }

    /// <summary>
    /// Last cancellable notification before this ViewModel leaves its
    /// current Region.
    /// </summary>
    protected virtual void OnRegionRemoving(
        RegionAssignmentChangingEventArgs e) {
    }

    /// <summary>
    /// Invoked after this ViewModel has left its source Region.
    /// </summary>
    public void RaiseRegionRemoved(
        RegionAssignmentContext context) {

        OnRegionRemoved(context);
    }

    protected virtual void OnRegionRemoved(
        RegionAssignmentContext context) {
    }

    #endregion Removal Lifecycle

    #region Assignment Lifecycle

    /// <summary>
    /// Invoked on the incoming ViewModel immediately before it is assigned
    /// to TargetRegionNode.
    ///
    /// Returning false cancels the complete assignment transaction.
    /// </summary>
    public bool RaiseRegionAssigning(
        RegionAssignmentContext context,
        bool currentDecision) {

        if (!CanAssignToRegion(context, currentDecision)) return false;

        var e = new RegionAssignmentChangingEventArgs(context);
        OnRegionAssigning(e);

        return !e.Cancel;
    }

    /// <summary>
    /// Last cancellable notification before this ViewModel occupies the
    /// target Region.
    /// </summary>
    protected virtual void OnRegionAssigning(
        RegionAssignmentChangingEventArgs e) {
    }

    /// <summary>
    /// Invoked after this ViewModel has been assigned to TargetRegionNode.
    /// </summary>
    public void RaiseRegionAssigned(
        RegionAssignmentContext context) {

        OnRegionAssigned(context);
    }

    protected virtual void OnRegionAssigned(
        RegionAssignmentContext context) {
    }

    #endregion Assignment Lifecycle
}