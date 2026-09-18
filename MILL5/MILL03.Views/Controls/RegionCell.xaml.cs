using Microsoft.Maui.Controls;
using MILL03.Views.UIInfrastructure;
using MILL06.ViewModels;
using MILL06.ViewModels.UIContracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MILL03.Views.Controls;

public enum SplitDirection { Top, Bottom, Left, Right }

public partial class RegionCell : ContentView {
    #region RegionNode

    public static readonly BindableProperty RegionNodeProperty = BindableProperty.Create(
        nameof(RegionNode),
        typeof(RegionNode),
        typeof(RegionCell),
        null,
        propertyChanged: (bindable, oldValue, newValue) => {
            if (bindable is not RegionCell cell) return;

            if (oldValue is RegionNode oldNode) {
                oldNode.PropertyChanged -= cell.OnNodePropertyChanged;
            }

            if (newValue is RegionNode newNode) {
                newNode.PropertyChanged += cell.OnNodePropertyChanged;
                cell.SyncWithNode(newNode);
            }
        });

    public RegionNode? RegionNode {
        get => (RegionNode?)GetValue(RegionNodeProperty);
        set => SetValue(RegionNodeProperty, value);
    }

    private void OnNodePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) {
        if (sender is RegionNode node && e.PropertyName == nameof(RegionNode.PayloadViewModel)) {
            MainThread.BeginInvokeOnMainThread(() => SyncWithNode(node));
        }
    }

    private void SyncWithNode(RegionNode node) {
        if (PayloadContainer == null) return;

        if (node.PayloadViewModel == null) {
            PayloadContainer.Content = new ContentView { BackgroundColor = GetNextColor() };
            return;
        }

        var resolvedView = ViewLocator.Instance.Resolve(node.PayloadViewModel);

        if (resolvedView != null && resolvedView.BackgroundColor == null) {
            resolvedView.BackgroundColor = Colors.White;
        }

        InjectPayload(resolvedView);
    }

    #endregion

    #region PayloadViewModel

    public static readonly BindableProperty PayloadViewModelProperty = BindableProperty.Create(
        nameof(PayloadViewModel),
        typeof(BaseViewModel),
        typeof(RegionCell),
        null,
        propertyChanged: (bindable, oldValue, newValue) => {
            if (bindable is RegionCell cell && newValue is BaseViewModel vm) {
                var resolvedView = ViewLocator.Instance.Resolve(vm);
                cell.InjectPayload(resolvedView);
            }
        });

    public BaseViewModel? PayloadViewModel {
        get => (BaseViewModel?)GetValue(PayloadViewModelProperty);
        set => SetValue(PayloadViewModelProperty, value);
    }

    #endregion

    #region Payload

    public static readonly BindableProperty PayloadProperty = BindableProperty.Create(
        nameof(Payload),
        typeof(View),
        typeof(RegionCell),
        null,
        propertyChanged: (bindable, oldValue, newValue) => {
            if (newValue is View newView) {
                ((RegionCell)bindable).InjectPayload(newView);
            }
        });

    public View Payload {
        get => (View)GetValue(PayloadProperty);
        set => SetValue(PayloadProperty, value);
    }

    #endregion

    #region IsSplitting

    public static readonly BindableProperty IsSplittingProperty = BindableProperty.Create(
        nameof(IsSplitting),
        typeof(bool),
        typeof(RegionCell),
        false,
        BindingMode.TwoWay,
        propertyChanged: OnIsSplittingStateChanged);

    public bool IsSplitting {
        get => (bool)GetValue(IsSplittingProperty);
        set => SetValue(IsSplittingProperty, value);
    }

    private static void OnIsSplittingStateChanged(BindableObject bindable, object oldValue, object newValue) {
        var cell = (RegionCell)bindable;
        bool isSplitting = (bool)newValue;
        var targetColor = isSplitting ? Color.FromArgb("#80808080") : Colors.Transparent;

        foreach (var adorner in cell._adorners.Value.Values) {
            adorner.BackgroundColor = targetColor;
        }

        cell.IndicatorLabel.IsVisible = isSplitting;
        cell.SplitPreview.IsVisible = isSplitting;
    }

    #endregion

    #region Fields

    private static readonly Color[] RegionColors = {
        Colors.White, Colors.LightBlue, Colors.LightCoral, Colors.LightGreen,
        Colors.LightGoldenrodYellow, Colors.LightPink, Colors.MediumPurple,
        Colors.LightSeaGreen, Colors.Orange, Colors.LightSkyBlue, Colors.Plum
    };

    private static readonly Random _random = new();
    private static int _lastColorIndex = -1;

    private readonly Lazy<Dictionary<SplitDirection, BoxView>> _adorners;

    #endregion

    #region Constructors

    public RegionCell() {
        InitializeComponent();

        _adorners = new Lazy<Dictionary<SplitDirection, BoxView>>(() => new Dictionary<SplitDirection, BoxView> {
            { SplitDirection.Top, TopAdorner },
            { SplitDirection.Bottom, BottomAdorner },
            { SplitDirection.Left, LeftAdorner },
            { SplitDirection.Right, RightAdorner }
        });

        WireUpAutonomousLogic();
    }

    #endregion

    #region Lifecycle

    protected override void OnHandlerChanged() {
        base.OnHandlerChanged();

        // Binding may have fired before InitializeComponent completed.
        if (Handler != null && RegionNode != null) {
            SyncWithNode(RegionNode);
        }
    }

    #endregion

    #region Split Interaction

    private void WireUpAutonomousLogic() {
        foreach (var kvp in _adorners.Value) {
            var direction = kvp.Key;
            var adorner = kvp.Value;

            var pointerGesture = new PointerGestureRecognizer();

            pointerGesture.PointerEntered += (s, e) => IsSplitting = true;
            pointerGesture.PointerExited += (s, e) => IsSplitting = false;
            pointerGesture.PointerMoved += (s, e) => TrackInteractions(e, direction);

            adorner.GestureRecognizers.Add(pointerGesture);

            var tapGesture = new TapGestureRecognizer();
            bool isTopBottom = direction == SplitDirection.Top || direction == SplitDirection.Bottom;

            tapGesture.Tapped += (s, e) => {
                var position = e.GetPosition(RootGrid);
                if (position.HasValue) PerformSplit(isTopBottom, position.Value.X, position.Value.Y);
            };

            adorner.GestureRecognizers.Add(tapGesture);
        }
    }

    private void TrackInteractions(PointerEventArgs e, SplitDirection direction) {
        var position = e.GetPosition(RootGrid);
        if (!position.HasValue) return;

        if (IndicatorLabel.IsVisible) {
            IndicatorLabel.TranslationX = position.Value.X + 8;
            IndicatorLabel.TranslationY = position.Value.Y - 9;
        }

        if (!SplitPreview.IsVisible) return;

        bool isVertical = direction == SplitDirection.Top || direction == SplitDirection.Bottom;

        if (isVertical) {
            SplitPreview.Orientation = SplitDividerVisual.DividerOrientation.Vertical;
            SplitPreview.WidthRequest = 5;
            SplitPreview.HeightRequest = -1;
            SplitPreview.HorizontalOptions = LayoutOptions.Start;
            SplitPreview.VerticalOptions = LayoutOptions.Fill;
            SplitPreview.TranslationX = position.Value.X - 2.5;
            SplitPreview.TranslationY = 0;
        }
        else {
            SplitPreview.Orientation = SplitDividerVisual.DividerOrientation.Horizontal;
            SplitPreview.WidthRequest = -1;
            SplitPreview.HeightRequest = 5;
            SplitPreview.HorizontalOptions = LayoutOptions.Fill;
            SplitPreview.VerticalOptions = LayoutOptions.Start;
            SplitPreview.TranslationX = 0;
            SplitPreview.TranslationY = position.Value.Y - 2.5;
        }
    }

    private void PerformSplit(bool isTopBottomClick, double clickX, double clickY) {
        var extractedPayload = PayloadContainer.Content ?? new ContentView { BackgroundColor = GetNextColor() };
        PayloadContainer.Content = null;

        RootGrid.Children.Clear();
        RootGrid.RowDefinitions.Clear();
        RootGrid.ColumnDefinitions.Clear();

        var splitGrid = new Grid();

        RegionNode? child1Node = null;
        RegionNode? child2Node = null;

        if (RegionNode != null) {
            double stateStar1 = isTopBottomClick ? clickX / RootGrid.Width : clickY / RootGrid.Height;
            double stateStar2 = 1.0 - stateStar1;

            child1Node = new RegionNode { PayloadViewModel = RegionNode.PayloadViewModel };
            child2Node = new RegionNode { PayloadViewModel = null };

            RegionNode.PayloadViewModel = null;
            RegionNode.FirstChildWeight = stateStar1;
            RegionNode.SecondChildWeight = stateStar2;
            RegionNode.FirstChild = child1Node;
            RegionNode.SecondChild = child2Node;
            RegionNode.Orientation = isTopBottomClick ? SplitOrientation.Vertical : SplitOrientation.Horizontal;
        }

        var cell1 = new RegionCell();
        if (child1Node != null) cell1.RegionNode = child1Node;
        cell1.InjectPayload(extractedPayload);

        var cell2 = new RegionCell();
        if (child2Node != null) cell2.RegionNode = child2Node;
        cell2.InjectPayload(new ContentView { BackgroundColor = GetNextColor() });

        var splitter = new GridSplitter {
            Orientation = isTopBottomClick
                ? GridSplitter.SplitOrientation.Vertical
                : GridSplitter.SplitOrientation.Horizontal,

            WidthRequest = isTopBottomClick ? 5 : -1,
            HeightRequest = isTopBottomClick ? -1 : 5,

            HorizontalOptions = isTopBottomClick ? LayoutOptions.Center : LayoutOptions.Fill,
            VerticalOptions = isTopBottomClick ? LayoutOptions.Fill : LayoutOptions.Center
        };

        if (isTopBottomClick) {
            double star1 = clickX / RootGrid.Width;
            double star2 = 1.0 - star1;

            splitGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(star1, GridUnitType.Star)));
            splitGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            splitGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(star2, GridUnitType.Star)));

            Grid.SetColumn(cell1, 0);
            Grid.SetColumn(splitter, 1);
            Grid.SetColumn(cell2, 2);
        }
        else {
            double star1 = clickY / RootGrid.Height;
            double star2 = 1.0 - star1;

            splitGrid.RowDefinitions.Add(new RowDefinition(new GridLength(star1, GridUnitType.Star)));
            splitGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            splitGrid.RowDefinitions.Add(new RowDefinition(new GridLength(star2, GridUnitType.Star)));

            Grid.SetRow(cell1, 0);
            Grid.SetRow(splitter, 1);
            Grid.SetRow(cell2, 2);
        }

        splitGrid.Children.Add(cell1);
        splitGrid.Children.Add(splitter);
        splitGrid.Children.Add(cell2);

        RootGrid.Children.Add(splitGrid);

        if (child2Node != null) {
            MainViewModel.Instance.ActiveRegionNode = child2Node;
        }
    }

    #endregion

    #region Close

    private void OnCloseButtonTapped(object sender, TappedEventArgs e) {
        if (Parent is not Grid splitGrid || RegionNode == null) return;

        var sibling = splitGrid.Children.OfType<RegionCell>().FirstOrDefault(cell => cell != this);
        if (sibling?.RegionNode == null) return;

        var ownerCell = FindOwningRegionCell(splitGrid);
        if (ownerCell?.RegionNode == null) return;

        var closedNode = RegionNode;
        var survivingNode = sibling.RegionNode;
        var parentNode = ownerCell.RegionNode;

        // The parent node survives. Promote the sibling's state into it.
        PromoteNode(survivingNode, parentNode);

        var splitter = splitGrid.Children.OfType<GridSplitter>().FirstOrDefault();
        if (splitter != null) splitGrid.Children.Remove(splitter);

        splitGrid.Children.Remove(this);

        splitGrid.ColumnDefinitions.Clear();
        splitGrid.RowDefinitions.Clear();

        Grid.SetColumn(sibling, 0);
        Grid.SetRow(sibling, 0);
        Grid.SetColumnSpan(sibling, 1);
        Grid.SetRowSpan(sibling, 1);

        // The surviving visual cell must now observe the promoted parent node.
        sibling.RegionNode = parentNode;

        var activeNode = MainViewModel.Instance.ActiveRegionNode;

        if (ReferenceEquals(activeNode, closedNode) ||
            ReferenceEquals(activeNode, survivingNode)) {
            MainViewModel.Instance.ActiveRegionNode = FindFirstLeaf(parentNode);
        }
    }

    private static void PromoteNode(RegionNode source, RegionNode target) {
        target.PayloadViewModel = source.PayloadViewModel;
        target.FirstChildWeight = source.FirstChildWeight;
        target.SecondChildWeight = source.SecondChildWeight;
        target.FirstChild = source.FirstChild;
        target.SecondChild = source.SecondChild;
        target.Orientation = source.Orientation;
    }

    private static RegionNode FindFirstLeaf(RegionNode node) {
        if (node.FirstChild == null && node.SecondChild == null) return node;
        if (node.FirstChild != null) return FindFirstLeaf(node.FirstChild);

        return FindFirstLeaf(node.SecondChild!);
    }

    private static RegionCell? FindOwningRegionCell(Element element) {
        Element? current = element.Parent;

        while (current != null) {
            if (current is RegionCell cell) return cell;
            current = current.Parent;
        }

        return null;
    }

    #endregion

    #region Payload

    public void InjectPayload(View payload) {
        PayloadContainer.Content = payload;
    }

    #endregion

    #region Colors

    public static Color GetNextColor() {
        int nextIndex;

        if (_lastColorIndex == -1) {
            nextIndex = 0;
        }
        else {
            do {
                nextIndex = _random.Next(RegionColors.Length);
            } while (nextIndex == _lastColorIndex);
        }

        _lastColorIndex = nextIndex;
        return RegionColors[nextIndex];
    }

    #endregion
}