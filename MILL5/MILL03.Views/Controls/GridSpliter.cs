using Microsoft.Maui.Controls;

namespace MILL03.Views.Controls;

public class GridSplitter : BoxView {
    public enum SplitOrientation { Vertical, Horizontal }

    public static readonly BindableProperty OrientationProperty = BindableProperty.Create(
        nameof(Orientation),
        typeof(SplitOrientation),
        typeof(GridSplitter),
        SplitOrientation.Vertical);

    public SplitOrientation Orientation {
        get => (SplitOrientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    private double _initialSize1;
    private double _initialSize2;
    private View? _element1;
    private View? _element2;

    public GridSplitter() {
        BackgroundColor = Color.FromArgb("#80808080");
        ZIndex = 100;

        var panGesture = new PanGestureRecognizer();
        panGesture.PanUpdated += OnPanUpdated;
        GestureRecognizers.Add(panGesture);

        // Hook into the native platform view to change the cursor
        this.HandlerChanged += OnHandlerChanged;
    }

    private void OnHandlerChanged(object? sender, EventArgs e) {
#if WINDOWS
        if (this.Handler?.PlatformView is Microsoft.UI.Xaml.UIElement platformView) {
            platformView.PointerEntered += (s, args) => {
                var shape = this.Orientation == SplitOrientation.Vertical
                    ? Microsoft.UI.Input.InputSystemCursorShape.SizeWestEast
                    : Microsoft.UI.Input.InputSystemCursorShape.SizeNorthSouth;

                var cursor = Microsoft.UI.Input.InputSystemCursor.Create(shape);

                // Use reflection to bypass the "protected" access modifier in WinUI 3
                typeof(Microsoft.UI.Xaml.UIElement).InvokeMember(
                    "ProtectedCursor",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance,
                    null,
                    platformView,
                    new object[] { cursor });
            };

            platformView.PointerExited += (s, args) => {
                var cursor = Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Arrow);

                typeof(Microsoft.UI.Xaml.UIElement).InvokeMember(
                    "ProtectedCursor",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance,
                    null,
                    platformView,
                    new object[] { cursor });
            };
        }
#endif
    }


    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e) {
        if (Parent is not Grid parentGrid) return;

        bool isVertical = Orientation == SplitOrientation.Vertical;
        int index = isVertical ? Grid.GetColumn(this) : Grid.GetRow(this);

        // Prevent crashes if the splitter is misplaced
        if (isVertical && (index <= 0 || index >= parentGrid.ColumnDefinitions.Count - 1)) return;
        if (!isVertical && (index <= 0 || index >= parentGrid.RowDefinitions.Count - 1)) return;

        if (e.StatusType == GestureStatus.Started) {
            // Interrogate the visual tree to find the adjacent payload containers
            _element1 = parentGrid.Children.OfType<View>().FirstOrDefault(c =>
                (isVertical ? Grid.GetColumn(c) : Grid.GetRow(c)) == index - 1);

            _element2 = parentGrid.Children.OfType<View>().FirstOrDefault(c =>
                (isVertical ? Grid.GetColumn(c) : Grid.GetRow(c)) == index + 1);

            if (_element1 != null && _element2 != null) {
                _initialSize1 = isVertical ? _element1.Width : _element1.Height;
                _initialSize2 = isVertical ? _element2.Width : _element2.Height;
            }
        }
        else if (e.StatusType == GestureStatus.Running && _element1 != null && _element2 != null) {
            if (isVertical) {
                double newSize1 = Math.Max(20, _initialSize1 + e.TotalX);
                double newSize2 = Math.Max(20, _initialSize2 - e.TotalX);

                parentGrid.ColumnDefinitions[index - 1].Width = new GridLength(newSize1, GridUnitType.Star);
                parentGrid.ColumnDefinitions[index + 1].Width = new GridLength(newSize2, GridUnitType.Star);
            }
            else {
                double newSize1 = Math.Max(20, _initialSize1 + e.TotalY);
                double newSize2 = Math.Max(20, _initialSize2 - e.TotalY);

                parentGrid.RowDefinitions[index - 1].Height = new GridLength(newSize1, GridUnitType.Star);
                parentGrid.RowDefinitions[index + 1].Height = new GridLength(newSize2, GridUnitType.Star);
            }
        }
    }
}