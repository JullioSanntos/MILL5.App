using Microsoft.Maui.Controls;
using MILL03.Views.UIInfrastructure;
using MILL06.ViewModels;
using MILL06.ViewModels.UIContracts;
using System;
using System.ComponentModel;
using System.Linq;

namespace MILL03.Views.Controls;

public partial class RegionCell : ContentView {
    #region RegionNode

    public static readonly BindableProperty RegionNodeProperty = BindableProperty.Create(
        nameof(RegionNode), typeof(RegionNode), typeof(RegionCell), null,
        propertyChanged: OnRegionNodeChanged);

    public RegionNode? RegionNode {
        get => (RegionNode?)GetValue(RegionNodeProperty);
        set => SetValue(RegionNodeProperty, value);
    }

    private static void OnRegionNodeChanged(BindableObject bindable, object oldValue, object newValue) {
        if (bindable is not RegionCell cell) return;

        if (oldValue is RegionNode oldNode) oldNode.PropertyChanged -= cell.OnNodePropertyChanged;

        if (newValue is RegionNode newNode) {
            newNode.PropertyChanged += cell.OnNodePropertyChanged;
            cell.SyncWithNode(newNode);
        }
    }

    private void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs e) {
        if (sender is RegionNode node && e.PropertyName == nameof(RegionNode.PayloadViewModel)) {
            MainThread.BeginInvokeOnMainThread(() => SyncWithNode(node));
        }
    }

    private void SyncWithNode(RegionNode node) {
        if (PayloadContainer == null) return;

        if (node.PayloadViewModel == null) {
            InjectPayload(CreateEmptyPayload());
            return;
        }

        var view = ViewLocator.Instance.Resolve(node.PayloadViewModel);
        if (view == null) return;

        if (view.BackgroundColor == null) view.BackgroundColor = Colors.White;

        InjectPayload(view);
    }

    #endregion RegionNode

    #region Direct Payload Bindings

    // Retained for direct RegionCell use that does not go through RegionNode.
    public static readonly BindableProperty PayloadViewModelProperty = BindableProperty.Create(
        nameof(PayloadViewModel), typeof(BaseViewModel), typeof(RegionCell), null,
        propertyChanged: OnPayloadViewModelChanged);

    public BaseViewModel? PayloadViewModel {
        get => (BaseViewModel?)GetValue(PayloadViewModelProperty);
        set => SetValue(PayloadViewModelProperty, value);
    }

    private static void OnPayloadViewModelChanged(BindableObject bindable, object oldValue, object newValue) {
        if (bindable is not RegionCell cell || newValue is not BaseViewModel viewModel) return;

        var view = ViewLocator.Instance.Resolve(viewModel);
        if (view != null) cell.InjectPayload(view);
    }

    public static readonly BindableProperty PayloadProperty = BindableProperty.Create(
        nameof(Payload), typeof(View), typeof(RegionCell), null,
        propertyChanged: OnPayloadChanged);

    public View Payload {
        get => (View)GetValue(PayloadProperty);
        set => SetValue(PayloadProperty, value);
    }

    private static void OnPayloadChanged(BindableObject bindable, object oldValue, object newValue) {
        if (bindable is RegionCell cell && newValue is View view) cell.InjectPayload(view);
    }

    #endregion Direct Payload Bindings

    #region Region Interaction Policy
    #region AllowsMoveProperty
    /// <summary>
    /// Indicates whether content occupying this layout position may normally
    /// be moved to another Region.
    ///
    /// This is layout policy, not drag-gesture behavior. A MenuNodeViewModel,
    /// for example, may be draggable even though it is not movable Region
    /// content.
    ///
    /// RegionBaseViewModel.IsMovable supplies the content-level policy, while
    /// CanRemoveFromRegion provides contextual exceptions.
    /// </summary>
    public static readonly BindableProperty AllowsMoveProperty =
        BindableProperty.Create(
            nameof(AllowsMove),
            typeof(bool),
            typeof(RegionCell),
            true);

    public bool AllowsMove {
        get => (bool)GetValue(AllowsMoveProperty);
        set => SetValue(AllowsMoveProperty, value);
    }
    #endregion AllowsMoveProperty

    #region AllowsDropProperty
    /// <summary>
    /// Indicates whether this layout position normally permits its current
    /// content to be replaced.
    ///
    /// This is the persistent layout default. It is not changed temporarily
    /// during drag-over. Candidate-specific exceptions are evaluated through
    /// RegionBaseViewModel.CanBeReplacedBy.
    /// </summary>
    public static readonly BindableProperty AllowsDropProperty =
        BindableProperty.Create(
            nameof(AllowsDrop),
            typeof(bool),
            typeof(RegionCell),
            true);

    public bool AllowsDrop {
        get => (bool)GetValue(AllowsDropProperty);
        set => SetValue(AllowsDropProperty, value);
    }
    #endregion AllowsDropProperty


    #endregion Region Interaction Policy

    #region Constructors

    public RegionCell() {
        InitializeComponent();
        SplitPerimeter.SplitRequested += SplitPerimeter_SplitRequested;
    }

    private void SplitPerimeter_SplitRequested(object? sender, SplitRequestedEventArgs e) {
        PerformSplit(e.Direction, e.Position);
    }

    #endregion Constructors

    #region Lifecycle

    protected override void OnHandlerChanged() {
        base.OnHandlerChanged();

        System.Diagnostics.Debug.WriteLine(
            $"RegionCell attached: Move={AllowsMove}, Drop={AllowsDrop}");

        // A binding may have assigned RegionNode before InitializeComponent completed.
        if (Handler != null && RegionNode != null) SyncWithNode(RegionNode);
    }

    #endregion Lifecycle

    #region Split

    private readonly record struct SplitNodes(RegionNode? First, RegionNode? Second);

    private void PerformSplit(SplitDirection direction, Point position) {
        if (RegionNode != null && !RegionNode.RaiseNodeChanging(RegionNode.Id, NodeAction.Splitting)) return;

        var firstWeight = GetSplitWeight(direction, position);
        var payload = ExtractPayload();
        var nodes = SplitNode(direction, firstWeight);
        var splitGrid = CreateSplitGrid(direction, firstWeight, nodes, payload);

        ReplaceWithSplitGrid(splitGrid);

        if (nodes.Second != null) MainViewModel.Instance.ActiveRegionNode = nodes.Second;
    }

    private double GetSplitWeight(SplitDirection direction, Point position) {
        return direction.IsVerticalSplit()
            ? position.X / RootGrid.Width
            : position.Y / RootGrid.Height;
    }

    private View ExtractPayload() {
        var payload = PayloadContainer.Content ?? CreateEmptyPayload();
        PayloadContainer.Content = null;

        return payload;
    }

    private SplitNodes SplitNode(SplitDirection direction, double firstWeight) {
        if (RegionNode == null) return default;

        var first = new RegionNode { PayloadViewModel = RegionNode.PayloadViewModel };
        var second = new RegionNode();

        RegionNode.PayloadViewModel = null;
        RegionNode.FirstChildWeight = firstWeight;
        RegionNode.SecondChildWeight = 1.0 - firstWeight;
        RegionNode.FirstChild = first;
        RegionNode.SecondChild = second;
        RegionNode.Orientation = direction.IsVerticalSplit()
            ? SplitOrientation.Vertical
            : SplitOrientation.Horizontal;

        return new SplitNodes(first, second);
    }

    private Grid CreateSplitGrid(
        SplitDirection direction,
        double firstWeight,
        SplitNodes nodes,
        View payload) {

        // The first child continues the existing Region's layout role.
        var firstCell = CreateCell(
            nodes.First,
            payload,
            AllowsMove,
            AllowsDrop);

        // A newly created empty Region begins as an ordinary destination.
        var secondCell = CreateCell(
            nodes.Second,
            CreateEmptyPayload());

        var splitter = CreateSplitter(direction);
        var grid = new Grid();

        ConfigureSplitGrid(
            grid,
            direction,
            firstWeight,
            firstCell,
            splitter,
            secondCell);

        grid.Children.Add(firstCell);
        grid.Children.Add(splitter);
        grid.Children.Add(secondCell);

        return grid;
    }

    private static RegionCell CreateCell(
        RegionNode? node,
        View payload,
        bool allowsMove = true,
        bool allowsDrop = true) {

        var cell = new RegionCell {
            AllowsMove = allowsMove,
            AllowsDrop = allowsDrop
        };

        System.Diagnostics.Debug.WriteLine(
            $"RegionCell created: Move={cell.AllowsMove}, Drop={cell.AllowsDrop}");

        if (node != null)
            cell.RegionNode = node;

        cell.InjectPayload(payload);

        return cell;
    }

    private static GridSplitter CreateSplitter(SplitDirection direction) {
        bool isVertical = direction.IsVerticalSplit();

        return new GridSplitter {
            Orientation = isVertical
                ? GridSplitter.SplitOrientation.Vertical
                : GridSplitter.SplitOrientation.Horizontal,

            HorizontalOptions = isVertical ? LayoutOptions.Center : LayoutOptions.Fill,
            VerticalOptions = isVertical ? LayoutOptions.Fill : LayoutOptions.Center
        };
    }

    private static void ConfigureSplitGrid(
        Grid grid, SplitDirection direction, double firstWeight,
        RegionCell firstCell, GridSplitter splitter, RegionCell secondCell) {

        if (direction.IsVerticalSplit()) {
            ConfigureColumns(grid, firstWeight);

            Grid.SetColumn(firstCell, 0);
            Grid.SetColumn(splitter, 1);
            Grid.SetColumn(secondCell, 2);
        }
        else {
            ConfigureRows(grid, firstWeight);

            Grid.SetRow(firstCell, 0);
            Grid.SetRow(splitter, 1);
            Grid.SetRow(secondCell, 2);
        }
    }

    private static void ConfigureColumns(Grid grid, double firstWeight) {
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(firstWeight, GridUnitType.Star)));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1.0 - firstWeight, GridUnitType.Star)));
    }

    private static void ConfigureRows(Grid grid, double firstWeight) {
        grid.RowDefinitions.Add(new RowDefinition(new GridLength(firstWeight, GridUnitType.Star)));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.RowDefinitions.Add(new RowDefinition(new GridLength(1.0 - firstWeight, GridUnitType.Star)));
    }

    private void ReplaceWithSplitGrid(Grid splitGrid) {
        RootGrid.Children.Clear();
        RootGrid.RowDefinitions.Clear();
        RootGrid.ColumnDefinitions.Clear();
        RootGrid.Children.Add(splitGrid);
    }

    #endregion Split

    #region Close

    private readonly record struct CloseContext(
        Grid SplitGrid,
        RegionCell OwnerCell,
        RegionCell Sibling,
        RegionNode ClosedNode,
        RegionNode SurvivingNode,
        RegionNode ParentNode);

    private void OnCloseButtonTapped(object sender, TappedEventArgs e) {
        if (!TryGetCloseContext(out var context)) return;
        if (!context.ClosedNode.RaiseNodeChanging(context.ClosedNode.Id, NodeAction.Closing)) return;

        PromoteNode(context.SurvivingNode, context.ParentNode);
        CollapseVisualTree(context);
        System.Diagnostics.Debug.WriteLine(
            $"After collapse: Move={context.OwnerCell.AllowsMove}, " +
            $"Drop={context.OwnerCell.AllowsDrop}");

        RepairActiveRegion(context);
    }

    private bool TryGetCloseContext(out CloseContext context) {
        context = default;

        if (Parent is not Grid splitGrid || RegionNode == null) return false;

        var sibling = splitGrid.Children
            .OfType<RegionCell>()
            .FirstOrDefault(cell => cell != this);

        if (sibling?.RegionNode == null) return false;

        var ownerCell = FindOwningRegionCell(splitGrid);
        if (ownerCell?.RegionNode == null) return false;

        context = new CloseContext(
            splitGrid,
            ownerCell,
            sibling,
            RegionNode,
            sibling.RegionNode,
            ownerCell.RegionNode);

        return true;
    }

    private static void PromoteNode(RegionNode source, RegionNode target) {
        target.PayloadViewModel = source.PayloadViewModel;
        target.FirstChildWeight = source.FirstChildWeight;
        target.SecondChildWeight = source.SecondChildWeight;
        target.FirstChild = source.FirstChild;
        target.SecondChild = source.SecondChild;
        target.Orientation = source.Orientation;
    }

    private void CollapseVisualTree(CloseContext context) {
        if (context.SurvivingNode.IsSplit) {
            PromoteSplitVisual(context);
        }
        else {
            PromoteLeafVisual(context);
        }

        // These RegionCells are no longer part of the visual/logical tree.
        // Detach their RegionNode subscriptions as well.
        context.Sibling.RegionNode = null;
        RegionNode = null;
    }

    private static void PromoteLeafVisual(CloseContext context) {
        var payload = context.Sibling.ExtractPayload();

        ClearRootGrid(context.OwnerCell);

        context.OwnerCell.RootGrid.Children.Add(
            context.OwnerCell.SplitPerimeter);

        context.OwnerCell.InjectPayload(payload);
    }

    private static void PromoteSplitVisual(CloseContext context) {
        var survivingSplitGrid = context.Sibling.RootGrid.Children
            .OfType<Grid>()
            .FirstOrDefault();

        if (survivingSplitGrid == null) {
            throw new InvalidOperationException(
                "A split RegionNode must be represented by a split Grid.");
        }

        // Detach it from the surviving child before removing the old
        // outer split hierarchy.
        context.Sibling.RootGrid.Children.Remove(survivingSplitGrid);

        ClearRootGrid(context.OwnerCell);

        context.OwnerCell.RootGrid.Children.Add(survivingSplitGrid);
    }

    private static void ClearRootGrid(RegionCell cell) {
        cell.RootGrid.Children.Clear();
        cell.RootGrid.ColumnDefinitions.Clear();
        cell.RootGrid.RowDefinitions.Clear();
    }

    private static void RepairActiveRegion(CloseContext context) {
        var mainViewModel = MainViewModel.Instance;
        var activeNode = mainViewModel.ActiveRegionNode;

        if (ReferenceEquals(activeNode, context.ClosedNode) ||
            ReferenceEquals(activeNode, context.SurvivingNode)) {
            mainViewModel.ActiveRegionNode = FindFirstLeaf(context.ParentNode);
        }
    }

    private static RegionNode FindFirstLeaf(RegionNode node) {
        if (node.FirstChild == null && node.SecondChild == null) return node;
        if (node.FirstChild != null) return FindFirstLeaf(node.FirstChild);

        return FindFirstLeaf(node.SecondChild!);
    }

    private static RegionCell? FindOwningRegionCell(Element element) {
        for (var current = element.Parent; current != null; current = current.Parent) {
            if (current is RegionCell cell) return cell;
        }

        return null;
    }

    #endregion Close

    #region Payload

    public void InjectPayload(View payload) {
        PayloadContainer.Content = payload;
    }

    private static ContentView CreateEmptyPayload() {
        return new ContentView { BackgroundColor = GetNextColor() };
    }

    #endregion Payload

    #region Colors

    private static readonly Color[] RegionColors = {
        Colors.White, Colors.LightBlue, Colors.LightCoral, Colors.LightGreen,
        Colors.LightGoldenrodYellow, Colors.LightPink, Colors.MediumPurple,
        Colors.LightSeaGreen, Colors.Orange, Colors.LightSkyBlue, Colors.Plum
    };

    private static readonly Random _random = new();
    private static int _lastColorIndex = -1;

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

    #endregion Colors
}