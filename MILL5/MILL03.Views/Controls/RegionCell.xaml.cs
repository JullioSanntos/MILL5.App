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

    private static void OnRegionNodeChanged(
        BindableObject bindable, object oldValue, object newValue) {

        if (bindable is not RegionCell cell) return;

        if (oldValue is RegionNode oldNode)
            oldNode.PropertyChanged -= cell.OnNodePropertyChanged;

        if (newValue is RegionNode newNode) {
            newNode.PropertyChanged += cell.OnNodePropertyChanged;
            cell.SyncWithNode(newNode);
        }
    }

    private void OnNodePropertyChanged(
        object? sender, PropertyChangedEventArgs e) {

        if (sender is RegionNode node &&
            e.PropertyName == nameof(RegionNode.PayloadViewModel)) {

            MainThread.BeginInvokeOnMainThread(
                () => SyncWithNode(node));
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

        if (view.BackgroundColor == null)
            view.BackgroundColor = Colors.White;

        InjectPayload(view);
    }

    #endregion RegionNode

    #region Constructors

    public RegionCell() {
        InitializeComponent();
        SplitPerimeter.SplitRequested += SplitPerimeter_SplitRequested;
    }

    private void SplitPerimeter_SplitRequested(
        object? sender, SplitRequestedEventArgs e) {

        PerformSplit(e.Direction, e.Position);
    }

    #endregion Constructors

    #region Lifecycle

    protected override void OnHandlerChanged() {
        base.OnHandlerChanged();

        if (Handler != null && RegionNode != null)
            SyncWithNode(RegionNode);
    }

    #endregion Lifecycle

    #region Visual Placement

    private readonly record struct GridPlacement(
        int Row,
        int Column,
        int RowSpan,
        int ColumnSpan);

    private static GridPlacement CapturePlacement(View view) {
        return new GridPlacement(
            Grid.GetRow(view),
            Grid.GetColumn(view),
            Grid.GetRowSpan(view),
            Grid.GetColumnSpan(view));
    }

    private static void ApplyPlacement(
        View view, GridPlacement placement) {

        Grid.SetRow(view, placement.Row);
        Grid.SetColumn(view, placement.Column);
        Grid.SetRowSpan(view, placement.RowSpan);
        Grid.SetColumnSpan(view, placement.ColumnSpan);
    }

    #endregion Visual Placement

    #region Split

    private void PerformSplit(
        SplitDirection direction, Point position) {

        var node = RegionNode;

        if (node == null ||
            Parent is not Grid parentGrid) {

            return;
        }

        if (!node.RaiseNodeChanging(
                node.Id,
                NodeAction.Splitting)) {

            return;
        }

        var placement = CapturePlacement(this);
        var firstWeight = GetSplitWeight(direction, position);

        var orientation = direction.IsVerticalSplit()
            ? SplitOrientation.Vertical
            : SplitOrientation.Horizontal;

        var split = RegionNodesTree.Instance.SplitNode(
            node,
            orientation,
            firstWeight);

        var branchCell = new RegionCell {
            RegionNode = split.BranchNode
        };

        var emptyCell = new RegionCell {
            RegionNode = split.EmptyNode
        };

        parentGrid.Children.Remove(this);

        var splitGrid = CreateSplitGrid(
            direction,
            firstWeight,
            this,
            emptyCell);

        branchCell.ReplaceWithSplitGrid(splitGrid);

        ApplyPlacement(branchCell, placement);
        parentGrid.Children.Add(branchCell);

        RegionNodesTree.Instance.NotifyTreeChanged();
    }

    private double GetSplitWeight(
        SplitDirection direction,
        Point position) {

        return direction.IsVerticalSplit()
            ? position.X / RootGrid.Width
            : position.Y / RootGrid.Height;
    }

    private static Grid CreateSplitGrid(
        SplitDirection direction,
        double firstWeight,
        RegionCell firstCell,
        RegionCell secondCell) {

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

    private static GridSplitter CreateSplitter(
        SplitDirection direction) {

        var isVertical = direction.IsVerticalSplit();

        return new GridSplitter {
            Orientation = isVertical
                ? GridSplitter.SplitOrientation.Vertical
                : GridSplitter.SplitOrientation.Horizontal,

            HorizontalOptions = isVertical
                ? LayoutOptions.Center
                : LayoutOptions.Fill,

            VerticalOptions = isVertical
                ? LayoutOptions.Fill
                : LayoutOptions.Center
        };
    }

    private static void ConfigureSplitGrid(
        Grid grid,
        SplitDirection direction,
        double firstWeight,
        RegionCell firstCell,
        GridSplitter splitter,
        RegionCell secondCell) {

        if (direction.IsVerticalSplit()) {
            ConfigureColumns(grid, firstWeight);

            Grid.SetColumn(firstCell, 0);
            Grid.SetColumn(splitter, 1);
            Grid.SetColumn(secondCell, 2);

            return;
        }

        ConfigureRows(grid, firstWeight);

        Grid.SetRow(firstCell, 0);
        Grid.SetRow(splitter, 1);
        Grid.SetRow(secondCell, 2);
    }

    private static void ConfigureColumns(
        Grid grid, double firstWeight) {

        grid.ColumnDefinitions.Add(
            new ColumnDefinition(
                new GridLength(
                    firstWeight,
                    GridUnitType.Star)));

        grid.ColumnDefinitions.Add(
            new ColumnDefinition(GridLength.Auto));

        grid.ColumnDefinitions.Add(
            new ColumnDefinition(
                new GridLength(
                    1.0 - firstWeight,
                    GridUnitType.Star)));
    }

    private static void ConfigureRows(
        Grid grid, double firstWeight) {

        grid.RowDefinitions.Add(
            new RowDefinition(
                new GridLength(
                    firstWeight,
                    GridUnitType.Star)));

        grid.RowDefinitions.Add(
            new RowDefinition(GridLength.Auto));

        grid.RowDefinitions.Add(
            new RowDefinition(
                new GridLength(
                    1.0 - firstWeight,
                    GridUnitType.Star)));
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
        Grid ParentGrid,
        GridPlacement OwnerPlacement,
        RegionCell OwnerCell,
        RegionCell Sibling,
        RegionNode ClosedNode);

    private void OnCloseButtonTapped(
        object sender,
        TappedEventArgs e) {

        if (!TryGetCloseContext(out var context))
            return;

        if (!context.ClosedNode.RaiseNodeChanging(
                context.ClosedNode.Id,
                NodeAction.Closing)) {

            return;
        }

        var survivingNode =
            RegionNodesTree.Instance.CloseNode(
                context.ClosedNode);

        if (survivingNode == null)
            return;

        if (!ReferenceEquals(
                survivingNode,
                context.Sibling.RegionNode)) {

            throw new InvalidOperationException(
                "The surviving RegionNode does not match the surviving RegionCell.");
        }

        PromoteSurvivingCell(context);

        RegionNode = null;
        context.OwnerCell.RegionNode = null;

        RegionNodesTree.Instance.NotifyTreeChanged();
    }

    private bool TryGetCloseContext(
        out CloseContext context) {

        context = default;

        if (Parent is not Grid splitGrid ||
            RegionNode == null) {

            return false;
        }

        var sibling = splitGrid.Children
            .OfType<RegionCell>()
            .FirstOrDefault(cell => cell != this);

        if (sibling?.RegionNode == null)
            return false;

        var ownerCell =
            FindOwningRegionCell(splitGrid);

        if (ownerCell?.RegionNode == null ||
            ownerCell.Parent is not Grid parentGrid) {

            return false;
        }

        context = new CloseContext(
            splitGrid,
            parentGrid,
            CapturePlacement(ownerCell),
            ownerCell,
            sibling,
            RegionNode);

        return true;
    }

    private static void PromoteSurvivingCell(
        CloseContext context) {

        context.SplitGrid.Children.Remove(
            context.Sibling);

        context.ParentGrid.Children.Remove(
            context.OwnerCell);

        ApplyPlacement(
            context.Sibling,
            context.OwnerPlacement);

        context.ParentGrid.Children.Add(
            context.Sibling);
    }

    private static RegionCell? FindOwningRegionCell(
        Element element) {

        for (var current = element.Parent;
             current != null;
             current = current.Parent) {

            if (current is RegionCell cell)
                return cell;
        }

        return null;
    }

    #endregion Close

    #region Payload

    public void InjectPayload(View payload) {
        PayloadContainer.Content = payload;
    }

    private static ContentView CreateEmptyPayload() {
        return new ContentView {
            BackgroundColor = GetNextColor()
        };
    }

    #endregion Payload

    #region Colors

    private static readonly Color[] RegionColors = {
        Colors.White,
        Colors.LightBlue,
        Colors.LightCoral,
        Colors.LightGreen,
        Colors.LightGoldenrodYellow,
        Colors.LightPink,
        Colors.MediumPurple,
        Colors.LightSeaGreen,
        Colors.Orange,
        Colors.LightSkyBlue,
        Colors.Plum
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
                nextIndex =
                    _random.Next(RegionColors.Length);

            } while (nextIndex == _lastColorIndex);
        }

        _lastColorIndex = nextIndex;
        return RegionColors[nextIndex];
    }

    #endregion Colors

    #region Drop

    private void RegionDropTarget_Dropped(
        object? sender,
        DropTargetDroppedEventArgs e) {

        switch (e.DragData) {
            case RegionCell sourceCell:
                if (e.Operation == DragDropOperation.Copy)
                    TryCopyRegion(sourceCell, this);
                else
                    TryMoveRegion(sourceCell, this);

                break;

            case MenuItemViewModel menuItem:
                TryAssignMenuItem(menuItem, RegionNode);
                break;
        }
    }

    private static bool TryMoveRegion(
        RegionCell sourceCell,
        RegionCell targetCell) {

        if (ReferenceEquals(sourceCell, targetCell))
            return false;

        var sourceNode = sourceCell.RegionNode;
        var targetNode = targetCell.RegionNode;

        if (sourceNode == null ||
            targetNode == null ||
            sourceCell.Parent is not Grid sourceGrid ||
            targetCell.Parent is not Grid targetGrid) {

            return false;
        }

        var sourcePlacement = CapturePlacement(sourceCell);
        var targetPlacement = CapturePlacement(targetCell);

        var result = RegionNodesTree.Instance.MoveRegion(
            sourceNode,
            targetNode);

        if (result == null)
            return false;

        var emptySourceCell = new RegionCell {
            RegionNode = result.Value.EmptySourceNode
        };

        sourceGrid.Children.Remove(sourceCell);
        targetGrid.Children.Remove(targetCell);

        targetCell.RegionNode = null;

        ApplyPlacement(
            emptySourceCell,
            sourcePlacement);

        ApplyPlacement(
            sourceCell,
            targetPlacement);

        sourceGrid.Children.Add(
            emptySourceCell);

        targetGrid.Children.Add(
            sourceCell);

        return true;
    }

    private static bool TryCopyRegion(
        RegionCell sourceCell,
        RegionCell targetCell) {

        var sourceNode = sourceCell.RegionNode;
        var targetNode = targetCell.RegionNode;

        if (sourceNode == null ||
            targetNode == null) {

            return false;
        }

        var viewModel =
            sourceNode.PayloadViewModel;

        if (viewModel == null)
            return false;

        return RegionNodesTree.Instance.AssignRegion(
            sourceNode,
            viewModel,
            targetNode);
    }

    private static bool TryAssignMenuItem(
        MenuItemViewModel menuItem,
        RegionNode? targetNode) {

        if (targetNode == null)
            return false;

        var viewModel =
            menuItem.TargetViewModel;

        if (viewModel == null)
            return false;

        var regions =
            RegionNodesTree.Instance;

        var sourceNode =
            regions.GetRegionNode(
                MainViewModel.Instance.MenuViewModel);

        if (sourceNode == null)
            return false;

        return regions.AssignRegion(
            sourceNode,
            viewModel,
            targetNode);
    }

    #endregion Drop
}