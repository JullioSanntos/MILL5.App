using Microsoft.Maui.Controls;

namespace MILL03.Views.Controls;

public enum SplitDirection { Top, Bottom, Left, Right }

public partial class RegionCell : ContentView {
    // Static Color Palette for visual debugging
    private static readonly Color[] RegionColors = {
        Colors.LightBlue, Colors.LightCoral, Colors.LightGreen, Colors.LightGoldenrodYellow,
        Colors.LightPink, Colors.MediumPurple, Colors.LightSeaGreen, Colors.Orange,
        Colors.LightSkyBlue, Colors.Plum
    };
    private static int _colorIndex = 0;

    public static Color GetNextColor() {
        var color = RegionColors[_colorIndex % RegionColors.Length];
        _colorIndex++;
        return color;
    }

    // MVVM Two-Way Bindable State
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

    // Lazy Cache for Adorners
    private readonly Lazy<Dictionary<SplitDirection, BoxView>> _adorners;

    public RegionCell() {
        InitializeComponent();

        _adorners = new Lazy<Dictionary<SplitDirection, BoxView>>(InitializeAdornerCache);

        WireUpAutonomousLogic();

        // Assign initial color to the root payload
        if (this.Content is Grid internalGrid) {
            var rootPayload = internalGrid.Children.FirstOrDefault(c => c is not BoxView && c is not Label) as ContentView;
            if (rootPayload != null && rootPayload.BackgroundColor == null) {
                rootPayload.BackgroundColor = GetNextColor();
            }
        }
    }

    private Dictionary<SplitDirection, BoxView> InitializeAdornerCache() {
        var dict = new Dictionary<SplitDirection, BoxView>();
        if (this.Content is Grid internalGrid) {
            foreach (var child in internalGrid.Children) {
                if (child is BoxView adorner) {
                    if (adorner.VerticalOptions == LayoutOptions.Start) dict[SplitDirection.Top] = adorner;
                    else if (adorner.VerticalOptions == LayoutOptions.End) dict[SplitDirection.Bottom] = adorner;
                    else if (adorner.HorizontalOptions == LayoutOptions.Start) dict[SplitDirection.Left] = adorner;
                    else if (adorner.HorizontalOptions == LayoutOptions.End) dict[SplitDirection.Right] = adorner;
                }
            }
        }
        return dict;
    }

    private static void OnIsSplittingStateChanged(BindableObject bindable, object oldValue, object newValue) {
        var cell = (RegionCell)bindable;
        bool isSplitting = (bool)newValue;

        var targetColor = isSplitting ? Color.FromArgb("#80808080") : Colors.Transparent;

        foreach (var adorner in cell._adorners.Value.Values) {
            adorner.BackgroundColor = targetColor;
        }

        if (cell.Content is Grid internalGrid && internalGrid.Children.FirstOrDefault(c => c is Label) is Label indicator) {
            indicator.IsVisible = isSplitting;
        }
    }

    private void WireUpAutonomousLogic() {
        if (this.Content is not Grid internalGrid) return;

        foreach (var kvp in _adorners.Value) {
            var direction = kvp.Key;
            var adorner = kvp.Value;

            var pointerGesture = new PointerGestureRecognizer();

            pointerGesture.PointerEntered += (s, e) => this.IsSplitting = true;
            pointerGesture.PointerExited += (s, e) => this.IsSplitting = false;
            pointerGesture.PointerMoved += (s, e) => TrackIndicator(internalGrid, e);

            adorner.GestureRecognizers.Add(pointerGesture);

            var tapGesture = new TapGestureRecognizer();
            bool isTopBottom = direction == SplitDirection.Top || direction == SplitDirection.Bottom;

            tapGesture.Tapped += (s, e) => PerformSplit(isTopBottom);
            adorner.GestureRecognizers.Add(tapGesture);
        }
    }

    private void TrackIndicator(Grid grid, PointerEventArgs e) {
        if (grid.Children.FirstOrDefault(c => c is Label) is Label indicator && indicator.IsVisible) {
            var position = e.GetPosition(grid);
            if (position.HasValue) {
                indicator.TranslationX = position.Value.X + 12;
                indicator.TranslationY = position.Value.Y + 12;
            }
        }
    }

    public void InjectPayload(View payload) {
        if (this.Content is not Grid internalGrid) return;
        var existingPayload = internalGrid.Children.FirstOrDefault(c => c is not BoxView && c is not Label);
        if (existingPayload != null) {
            internalGrid.Children.Remove(existingPayload);
        }
        internalGrid.Children.Insert(0, payload);
    }

    private void PerformSplit(bool isTopBottomClick) {
        if (this.Content is not Grid internalGrid) return;

        var payloadContainer = internalGrid.Children.FirstOrDefault(c => c is not BoxView && c is not Label) as View;
        if (payloadContainer == null) return;

        // Strip the cell of its interactive overlay elements permanently
        var uiElementsToRemove = internalGrid.Children.Where(c => c is BoxView || c is Label).ToList();
        foreach (var el in uiElementsToRemove) {
            internalGrid.Children.Remove(el);
        }
        internalGrid.Children.Remove(payloadContainer);

        var splitGrid = new Grid();
        var cell1 = new RegionCell();
        cell1.InjectPayload(payloadContainer);

        var cell2 = new RegionCell();

        if (isTopBottomClick) {
            // Vertical line creates Left/Right columns
            splitGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            splitGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            Grid.SetColumn(cell1, 0);
            Grid.SetColumn(cell2, 1);
        }
        else {
            // Horizontal line creates Top/Bottom rows
            splitGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            splitGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            Grid.SetRow(cell1, 0);
            Grid.SetRow(cell2, 1);
        }

        splitGrid.Children.Add(cell1);
        splitGrid.Children.Add(cell2);
        internalGrid.Children.Add(splitGrid);
    }
}