using Microsoft.Maui.Controls;

namespace MILL03.Views.Controls;

public partial class Perimeter : ContentView {
    private static readonly Color HoverColor = Color.FromArgb("#80808080");

    public event EventHandler<SplitRequestedEventArgs>? SplitRequested;

    #region InnerContent

    public static readonly BindableProperty InnerContentProperty = BindableProperty.Create(
        nameof(InnerContent),
        typeof(View),
        typeof(Perimeter),
        null,
        propertyChanged: OnInnerContentChanged);

    public View? InnerContent {
        get => (View?)GetValue(InnerContentProperty);
        set => SetValue(InnerContentProperty, value);
    }

    private static void OnInnerContentChanged(BindableObject bindable, object oldValue, object newValue) {
        ((Perimeter)bindable).InnerContentHost.Content = newValue as View;
    }

    #endregion InnerContent

    public Perimeter() {
        InitializeComponent();

        WireArea(TopArea, SplitDirection.Top);
        WireArea(BottomArea, SplitDirection.Bottom);
        WireArea(LeftArea, SplitDirection.Left);
        WireArea(RightArea, SplitDirection.Right);
    }

    private void WireArea(BoxView area, SplitDirection direction) {
        var pointerGesture = new PointerGestureRecognizer();

        pointerGesture.PointerEntered += (s, e) => ShowPreview(direction);
        pointerGesture.PointerExited += (s, e) => HidePreview();
        pointerGesture.PointerMoved += (s, e) => TrackPreview(e, direction);

        area.GestureRecognizers.Add(pointerGesture);

        var tapGesture = new TapGestureRecognizer();

        tapGesture.Tapped += (s, e) => {
            var position = e.GetPosition(RootGrid);
            if (position.HasValue) {
                SplitRequested?.Invoke(this, new SplitRequestedEventArgs(direction, position.Value));
            }
        };

        area.GestureRecognizers.Add(tapGesture);
    }

    private void ShowPreview(SplitDirection direction) {
        SetPerimeterColor(HoverColor);

        SplitPreview.IsVisible = true;
        IndicatorLabel.IsVisible = true;

        SetPreviewOrientation(direction);
    }

    private void HidePreview() {
        SetPerimeterColor(Colors.Transparent);

        SplitPreview.IsVisible = false;
        IndicatorLabel.IsVisible = false;
    }

    private void TrackPreview(PointerEventArgs e, SplitDirection direction) {
        var position = e.GetPosition(RootGrid);
        if (!position.HasValue) return;

        IndicatorLabel.TranslationX = position.Value.X + 8;
        IndicatorLabel.TranslationY = position.Value.Y - 9;

        SetPreviewOrientation(direction);

        if (IsVertical(direction)) {
            SplitPreview.TranslationX = position.Value.X - 2.5;
            SplitPreview.TranslationY = 0;
        }
        else {
            SplitPreview.TranslationX = 0;
            SplitPreview.TranslationY = position.Value.Y - 2.5;
        }
    }

    private void SetPreviewOrientation(SplitDirection direction) {
        if (IsVertical(direction)) {
            SplitPreview.Orientation = SplitDividerVisual.DividerOrientation.Vertical;
            SplitPreview.WidthRequest = 5;
            SplitPreview.HeightRequest = -1;
            SplitPreview.HorizontalOptions = LayoutOptions.Start;
            SplitPreview.VerticalOptions = LayoutOptions.Fill;
        }
        else {
            SplitPreview.Orientation = SplitDividerVisual.DividerOrientation.Horizontal;
            SplitPreview.WidthRequest = -1;
            SplitPreview.HeightRequest = 5;
            SplitPreview.HorizontalOptions = LayoutOptions.Fill;
            SplitPreview.VerticalOptions = LayoutOptions.Start;
        }
    }

    private void SetPerimeterColor(Color color) {
        TopArea.BackgroundColor = color;
        BottomArea.BackgroundColor = color;
        LeftArea.BackgroundColor = color;
        RightArea.BackgroundColor = color;
    }

    private static bool IsVertical(SplitDirection direction) {
        return direction == SplitDirection.Top ||
               direction == SplitDirection.Bottom;
    }
}