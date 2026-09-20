namespace MILL06.ViewModels.UIContracts;

/// <summary>
/// Coordinates Region assignment as a single transaction.
///
/// The coordinator does not own layout policy. Layout-level permissions are
/// supplied by the RegionCell participating in the operation.
///
/// PREVIEW / ELIGIBILITY:
///
///     Target ViewModel   -> CanBeReplacedBy
///     Source ViewModel   -> CanRemoveFromRegion     (Move only)
///     Incoming ViewModel -> CanAssignToRegion
///
/// COMMIT:
///
///     Target ViewModel   -> RegionReplacing
///     Source ViewModel   -> RegionRemoving          (Move only)
///     Incoming ViewModel -> RegionAssigning
///
///     -------- RegionNode mutation --------
///
///     Target ViewModel   -> RegionReplaced
///     Source ViewModel   -> RegionRemoved           (Move only)
///     Incoming ViewModel -> RegionAssigned
///
/// Any applicable participant may veto before mutation. No participant owns
/// the final decision independently; the coordinator commits only when all
/// applicable participants approve.
/// </summary>
public static class RegionAssignmentCoordinator {
    #region Evaluation

    /// <summary>
    /// Evaluates whether the proposed assignment is currently valid.
    ///
    /// This method is side-effect free and may be called repeatedly while
    /// dragging over candidate Regions to determine visual eligibility.
    ///
    /// sourceAllowsMove describes the source RegionCell's layout policy.
    /// targetAllowsDrop describes the target RegionCell's layout policy.
    /// </summary>
    public static bool CanAssign(
        RegionAssignmentContext context,
        bool sourceAllowsMove,
        bool targetAllowsDrop) {

        if (IsNoOp(context)) return false;
        if (!CanReplaceTarget(context, targetAllowsDrop)) return false;
        if (!CanRemoveSource(context, sourceAllowsMove)) return false;

        return CanAssignIncoming(context);
    }

    private static bool IsNoOp(
        RegionAssignmentContext context) {

        if (ReferenceEquals(
            context.TargetRegionNode.PayloadViewModel,
            context.IncomingViewModel)) {

            return true;
        }

        return context.Operation == RegionAssignmentOperation.Move &&
               ReferenceEquals(
                   context.SourceRegionNode,
                   context.TargetRegionNode);
    }

    /// <summary>
    /// Evaluates the target side of the proposed assignment.
    ///
    /// targetAllowsDrop is the persistent layout-level default supplied by
    /// RegionCell. A RegionBaseViewModel occupying the target may preserve
    /// or contextually override that decision through CanBeReplacedBy().
    ///
    /// This allows, for example, a protected Menu Region to reject ordinary
    /// content while still permitting another Menu to replace it.
    /// </summary>
    private static bool CanReplaceTarget(
        RegionAssignmentContext context,
        bool targetAllowsDrop) {

        var targetViewModel =
            context.TargetRegionNode.PayloadViewModel;

        if (targetViewModel == null)
            return targetAllowsDrop;

        if (targetViewModel is not RegionBaseViewModel regionViewModel)
            return targetAllowsDrop;

        var currentDecision =
            targetAllowsDrop &&
            regionViewModel.IsReplaceable;

        return regionViewModel.CanBeReplacedBy(
            context,
            currentDecision);
    }

    /// <summary>
    /// Evaluates whether existing Region content may leave its source Region.
    ///
    /// This participates only in Move operations.
    ///
    /// "Movable" is intentionally different from "Draggable":
    /// a draggable MenuNodeViewModel may initiate an assignment request,
    /// while a movable RegionBaseViewModel represents existing Region content
    /// that can itself be relocated from one Region to another.
    /// </summary>
    private static bool CanRemoveSource(
        RegionAssignmentContext context,
        bool sourceAllowsMove) {

        if (context.Operation != RegionAssignmentOperation.Move)
            return true;

        var sourceViewModel =
            context.SourceRegionNode?.PayloadViewModel;

        if (sourceViewModel is not RegionBaseViewModel regionViewModel)
            return false;

        var currentDecision =
            sourceAllowsMove &&
            regionViewModel.IsMovable;

        return regionViewModel.CanRemoveFromRegion(
            context,
            currentDecision);
    }

    /// <summary>
    /// Gives the incoming ViewModel an opportunity to reject the proposed
    /// target Region.
    ///
    /// Target layout and replacement policy have already been evaluated,
    /// therefore the incoming ViewModel begins with an accepted decision.
    /// </summary>
    private static bool CanAssignIncoming(
        RegionAssignmentContext context) {

        return context.IncomingViewModel.CanAssignToRegion(
            context,
            true);
    }

    #endregion Evaluation

    #region Assignment

    /// <summary>
    /// Attempts the complete Region assignment transaction.
    ///
    /// Pure eligibility rules are evaluated first. If accepted, each
    /// applicable participant receives its cancellable pre-mutation
    /// lifecycle notification.
    ///
    /// RegionNode state changes only after every participant approves.
    /// </summary>
    public static bool TryAssign(
        RegionAssignmentContext context,
        bool sourceAllowsMove,
        bool targetAllowsDrop) {

        if (!CanAssign(
            context,
            sourceAllowsMove,
            targetAllowsDrop)) {

            return false;
        }

        var targetViewModel =
            context.TargetRegionNode.PayloadViewModel
                as RegionBaseViewModel;

        var sourceViewModel =
            context.SourceRegionNode?.PayloadViewModel
                as RegionBaseViewModel;

        if (!ConfirmTarget(
            context,
            targetViewModel,
            targetAllowsDrop)) {

            return false;
        }

        if (!ConfirmSource(
            context,
            sourceViewModel,
            sourceAllowsMove)) {

            return false;
        }

        if (!context.IncomingViewModel.RaiseRegionAssigning(
            context,
            true)) {

            return false;
        }

        ApplyAssignment(context);

        targetViewModel?.RaiseRegionReplaced(context);

        if (context.Operation == RegionAssignmentOperation.Move)
            sourceViewModel?.RaiseRegionRemoved(context);

        context.IncomingViewModel.RaiseRegionAssigned(context);

        return true;
    }

    /// <summary>
    /// Invokes the target's final cancellable lifecycle point.
    ///
    /// The target may still veto after preview validation but before any
    /// RegionNode mutation occurs.
    /// </summary>
    private static bool ConfirmTarget(
        RegionAssignmentContext context,
        RegionBaseViewModel? targetViewModel,
        bool targetAllowsDrop) {

        if (targetViewModel == null)
            return targetAllowsDrop;

        var currentDecision =
            targetAllowsDrop &&
            targetViewModel.IsReplaceable;

        return targetViewModel.RaiseRegionReplacing(
            context,
            currentDecision);
    }

    /// <summary>
    /// Invokes the source's final cancellable lifecycle point for a Move.
    ///
    /// Assign and Copy operations leave the source unchanged and therefore
    /// do not enter the RegionRemoving lifecycle.
    /// </summary>
    private static bool ConfirmSource(
        RegionAssignmentContext context,
        RegionBaseViewModel? sourceViewModel,
        bool sourceAllowsMove) {

        if (context.Operation != RegionAssignmentOperation.Move)
            return true;

        if (sourceViewModel == null)
            return false;

        var currentDecision =
            sourceAllowsMove &&
            sourceViewModel.IsMovable;

        return sourceViewModel.RaiseRegionRemoving(
            context,
            currentDecision);
    }

    /// <summary>
    /// Commit point.
    ///
    /// No participant may cancel once mutation begins.
    ///
    /// Move removes the incoming ViewModel from its source before assigning
    /// it to the target. Assign and Copy leave the source untouched.
    /// </summary>
    private static void ApplyAssignment(
        RegionAssignmentContext context) {

        if (context.Operation == RegionAssignmentOperation.Move &&
            context.SourceRegionNode != null &&
            !ReferenceEquals(
                context.SourceRegionNode,
                context.TargetRegionNode)) {

            context.SourceRegionNode.PayloadViewModel = null;
        }

        context.TargetRegionNode.PayloadViewModel =
            context.IncomingViewModel;
    }

    #endregion Assignment
}