using CommunityToolkit.Mvvm.ComponentModel;
using MILL06.ViewModels.UIContracts;

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

    [ObservableProperty]
    private string? _payloadKey;
    partial void OnPayloadKeyChanged(string? value) {
        OnPropertyChanged(nameof(IsOccupied));
    }

    public bool IsOccupied => !string.IsNullOrEmpty(PayloadKey) || IsSplit;

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
}