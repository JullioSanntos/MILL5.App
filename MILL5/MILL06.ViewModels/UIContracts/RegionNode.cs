using System;
using CommunityToolkit.Mvvm.ComponentModel;
using MILL06.ViewModels; // Added for BaseViewModel

namespace MILL06.ViewModels.UIContracts;

public partial class RegionNode : ObservableObject {
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString();

    [ObservableProperty]
    private SplitOrientation? _orientation;

    partial void OnOrientationChanged(SplitOrientation? value) {
        OnPropertyChanged(nameof(IsSplit));
        OnPropertyChanged(nameof(IsOccupied));
    }

    public bool IsSplit => Orientation.HasValue && FirstChild != null && SecondChild != null;

    [ObservableProperty]
    private double _firstChildWeight = 1.0;

    [ObservableProperty]
    private double _secondChildWeight = 1.0;

    [ObservableProperty]
    private double _width;

    [ObservableProperty]
    private double _height;

    [ObservableProperty]
    private RegionNode? _firstChild;
    partial void OnFirstChildChanged(RegionNode? value) {
        if (value != null) value.Parent = this;
        OnPropertyChanged(nameof(IsSplit));
        OnPropertyChanged(nameof(IsOccupied));
    }

    [ObservableProperty]
    private RegionNode? _secondChild;
    partial void OnSecondChildChanged(RegionNode? value) {
        if (value != null) value.Parent = this;
        OnPropertyChanged(nameof(IsSplit));
        OnPropertyChanged(nameof(IsOccupied));
    }

    [ObservableProperty]
    private RegionNode? _parent;

    // --- THE PIVOT: ViewModel instead of string key ---
    [ObservableProperty]
    private BaseViewModel? _payloadViewModel;

    private static long _nextCreationOrder;
    public long CreationOrder { get; } = Interlocked.Increment(ref _nextCreationOrder);

    partial void OnPayloadViewModelChanged(BaseViewModel? value) {
        OnPropertyChanged(nameof(IsOccupied));
    }

    // --- UPDATED: Now checks the object instead of the string ---
    public bool IsOccupied => PayloadViewModel != null || IsSplit;

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

    partial void OnFirstChildChanged(RegionNode? oldValue, RegionNode? newValue) {
        if (oldValue != null && ReferenceEquals(oldValue.Parent, this)) {
            oldValue.Parent = null;
        }

        if (newValue != null) {
            newValue.Parent = this;
        }

        OnPropertyChanged(nameof(IsSplit));
        OnPropertyChanged(nameof(IsOccupied));
    }

    partial void OnSecondChildChanged(RegionNode? oldValue, RegionNode? newValue) {
        if (oldValue != null && ReferenceEquals(oldValue.Parent, this)) {
            oldValue.Parent = null;
        }

        if (newValue != null) {
            newValue.Parent = this;
        }

        OnPropertyChanged(nameof(IsSplit));
        OnPropertyChanged(nameof(IsOccupied));
    }
}