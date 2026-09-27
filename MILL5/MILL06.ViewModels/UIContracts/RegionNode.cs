using System;
using System.ComponentModel;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using MILL06.ViewModels;

namespace MILL06.ViewModels.UIContracts;

public partial class RegionNode : ObservableObject {

    #region Identity

    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString();

    private static long _nextCreationOrder;

    public long CreationOrder { get; } =
        Interlocked.Increment(ref _nextCreationOrder);

    #endregion Identity

    #region Layout

    [ObservableProperty]
    private SplitOrientation? _orientation;

    [ObservableProperty]
    private double _firstChildWeight = 1.0;

    [ObservableProperty]
    private double _secondChildWeight = 1.0;

    [ObservableProperty]
    private double _width;

    [ObservableProperty]
    private double _height;

    partial void OnOrientationChanged(SplitOrientation? value) {
        OnPropertyChanged(nameof(IsSplit));
        OnPropertyChanged(nameof(IsOccupied));
    }

    #endregion Layout

    #region Children

    [ObservableProperty]
    private RegionNode? _firstChild;

    [ObservableProperty]
    private RegionNode? _secondChild;

    [ObservableProperty]
    private RegionNode? _parent;

    partial void OnFirstChildChanged(RegionNode? oldValue, RegionNode? newValue) {
        if (oldValue != null && ReferenceEquals(oldValue.Parent, this))
            oldValue.Parent = null;

        if (newValue != null)
            newValue.Parent = this;

        OnPropertyChanged(nameof(IsSplit));
        OnPropertyChanged(nameof(IsOccupied));
    }

    partial void OnSecondChildChanged(RegionNode? oldValue, RegionNode? newValue) {
        if (oldValue != null && ReferenceEquals(oldValue.Parent, this))
            oldValue.Parent = null;

        if (newValue != null)
            newValue.Parent = this;

        OnPropertyChanged(nameof(IsSplit));
        OnPropertyChanged(nameof(IsOccupied));
    }

    partial void OnParentChanged(RegionNode? value) {
        OnPropertyChanged(nameof(CanBeClosed));
    }

    public bool IsSplit =>
        Orientation.HasValue &&
        FirstChild != null &&
        SecondChild != null;

    #endregion Children

    #region Payload

    [ObservableProperty]
    private BaseViewModel? _payloadViewModel;

    partial void OnPayloadViewModelChanged(BaseViewModel? oldValue, BaseViewModel? newValue) {
        if (oldValue != null)
            oldValue.PropertyChanged -= PayloadViewModel_PropertyChanged;

        if (oldValue is RegionBaseViewModel oldRegionViewModel)
            oldRegionViewModel.DragDropCapabilitiesChanged -=
                PayloadViewModel_DragDropCapabilitiesChanged;

        if (newValue != null)
            newValue.PropertyChanged += PayloadViewModel_PropertyChanged;

        if (newValue is RegionBaseViewModel newRegionViewModel)
            newRegionViewModel.DragDropCapabilitiesChanged +=
                PayloadViewModel_DragDropCapabilitiesChanged;

        OnPropertyChanged(nameof(IsOccupied));
        OnPropertyChanged(nameof(CanBeClosed));

        ReevaluateOwnDragDropCapabilities();
    }

    private void PayloadViewModel_PropertyChanged(
        object? sender, PropertyChangedEventArgs e) {

        if (e.PropertyName == nameof(BaseViewModel.CanBeClosed))
            OnPropertyChanged(nameof(CanBeClosed));
    }

    public bool IsOccupied =>
        PayloadViewModel != null || IsSplit;

    #endregion Payload

    #region Drag Drop Capabilities

    public bool CanBeDragged =>
        PayloadViewModel is RegionBaseViewModel viewModel &&
        viewModel.CanBeDragged;

    public bool CanBeReplaced =>
        PayloadViewModel is not RegionBaseViewModel viewModel ||
        viewModel.CanBeReplaced;

    public bool CanBeClosed =>
        Parent != null &&
        (PayloadViewModel?.CanBeClosed ?? true);

    public void ReevaluateOwnDragDropCapabilities() {
        OnPropertyChanged(nameof(CanBeDragged));
        OnPropertyChanged(nameof(CanBeReplaced));
    }

    public void ReevaluateDragDropCapabilities() {
        ReevaluateOwnDragDropCapabilities();

        FirstChild?.ReevaluateDragDropCapabilities();
        SecondChild?.ReevaluateDragDropCapabilities();
    }

    private void PayloadViewModel_DragDropCapabilitiesChanged(
        object? sender, EventArgs e) {

        ReevaluateOwnDragDropCapabilities();
    }

    #endregion Drag Drop Capabilities

    #region Region Assignment

    public event EventHandler<RegionAssignedEventArgs>? RegionAssigned;

    public void RaiseRegionAssigned(
        RegionNode? sourceRegionNode,
        RegionNode targetRegionNode,
        BaseViewModel assignedViewModel) {

        var e = new RegionAssignedEventArgs(
            sourceRegionNode,
            targetRegionNode,
            assignedViewModel);

        RegionAssigned?.Invoke(this, e);
    }

    #endregion Region Assignment

    #region Node Changing

    public event EventHandler<RegionNodeChangingEventArgs>? NodeChanging;

    public bool RaiseNodeChanging(string nodeId, NodeAction action) {
        var args = new RegionNodeChangingEventArgs(nodeId, action);

        OnNodeChanging(args);

        return !args.Cancel;
    }

    protected virtual void OnNodeChanging(RegionNodeChangingEventArgs e) {
        NodeChanging?.Invoke(this, e);
        Parent?.OnNodeChanging(e);
    }

    #endregion Node Changing
}