using Microsoft.Maui.Controls;

namespace MILL03.Views.Controls;

public enum SplitDirection { Top, Bottom, Left, Right }

public partial class RegionCell : ContentView {
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

    private readonly Lazy<Dictionary<SplitDirection, BoxView>> _adorners;

    public RegionCell() {
        InitializeComponent();

        _adorners = new Lazy<Dictionary<SplitDirection, BoxView>>(InitializeAdornerCache);

        WireUpAutonomousLogic();

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
                // Ensure we only cache the perimeter hit-boxes, not the anticipation line
                if (child is BoxView adorner && adorner.StyleId != "AnticipationLine") {
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

        if (cell.Content is Grid internalGrid) {
            if (internalGrid.Children.FirstOrDefault(c => c is Label) is Label indicator)
                indicator.IsVisible = isSplitting;

            if (internalGrid.Children.FirstOrDefault(c => c is BoxView b && b.StyleId == "AnticipationLine") is BoxView line)
                line.IsVisible = isSplitting;
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
            pointerGesture.PointerMoved += (s, e) => TrackInteractions(internalGrid, e, direction);
            adorner.GestureRecognizers.Add(pointerGesture);

            var tapGesture = new TapGestureRecognizer();
            bool isTopBottom = direction == SplitDirection.Top || direction == SplitDirection.Bottom;

            tapGesture.Tapped += (s, e) => {
                var pos = e.GetPosition(internalGrid);
                if (pos.HasValue) PerformSplit(isTopBottom, pos.Value.X, pos.Value.Y);
            };

            adorner.GestureRecognizers.Add(tapGesture);
        }
    }

    private void TrackInteractions(Grid grid, PointerEventArgs e, SplitDirection direction) {
        var position = e.GetPosition(grid);
        if (!position.HasValue) return;

        // 1. Move the indicator
        if (grid.Children.FirstOrDefault(c => c is Label) is Label indicator && indicator.IsVisible) {
            indicator.TranslationX = position.Value.X + 8;
            indicator.TranslationY = position.Value.Y - 9;
        }

        // 2. Shape and move the anticipation line
        if (grid.Children.FirstOrDefault(c => c is BoxView b && b.StyleId == "AnticipationLine") is BoxView line && line.IsVisible) {
            if (direction == SplitDirection.Top || direction == SplitDirection.Bottom) {
                line.WidthRequest = 2;
                line.HeightRequest = -1; // Fill available height
                line.HorizontalOptions = LayoutOptions.Start;
                line.VerticalOptions = LayoutOptions.Fill;
                line.TranslationX = position.Value.X;
                line.TranslationY = 0;
            }
            else {
                line.HeightRequest = 2;
                line.WidthRequest = -1; // Fill available width
                line.VerticalOptions = LayoutOptions.Start;
                line.HorizontalOptions = LayoutOptions.Fill;
                line.TranslationY = position.Value.Y;
                line.TranslationX = 0;
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

    private void PerformSplit(bool isTopBottomClick, double clickX, double clickY) {
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
            double star1 = clickX / internalGrid.Width;
            double star2 = 1.0 - star1;

            splitGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(star1, GridUnitType.Star)));
            splitGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(star2, GridUnitType.Star)));
            Grid.SetColumn(cell1, 0);
            Grid.SetColumn(cell2, 1);
        }
        else {
            double star1 = clickY / internalGrid.Height;
            double star2 = 1.0 - star1;

            splitGrid.RowDefinitions.Add(new RowDefinition(new GridLength(star1, GridUnitType.Star)));
            splitGrid.RowDefinitions.Add(new RowDefinition(new GridLength(star2, GridUnitType.Star)));
            Grid.SetRow(cell1, 0);
            Grid.SetRow(cell2, 1);
        }

        splitGrid.Children.Add(cell1);
        splitGrid.Children.Add(cell2);
        internalGrid.Children.Add(splitGrid);
    }
}