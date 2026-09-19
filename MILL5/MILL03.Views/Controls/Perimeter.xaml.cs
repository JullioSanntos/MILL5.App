using Microsoft.Maui.Controls;

namespace MILL03.Views.Controls;

public partial class Perimeter : ContentView {
    private const double IndicatorOffsetX = 8;
    private const double IndicatorOffsetY = -9;
    private const double PreviewHalfThickness = 2.5;

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

        pointerGesture.PointerEntered += (s, e) => {
            SetHoverState(true);
            SetPreviewOrientation(direction);
        };

        pointerGesture.PointerExited += (s, e) => ResetInteractionState();
        pointerGesture.PointerMoved += (s, e) => TrackPreview(e, direction);

        area.GestureRecognizers.Add(pointerGesture);

        var tapGesture = new TapGestureRecognizer();

        tapGesture.Tapped += (s, e) => {
            var position = e.GetPosition(RootGrid);
            if (!position.HasValue) return;

            ResetInteractionState();

            SplitRequested?.Invoke(
                this,
                new SplitRequestedEventArgs(direction, position.Value));
        };

        area.GestureRecognizers.Add(tapGesture);
    }

    private void SetHoverState(bool isHovering) {
        if (!isHovering) {
            ResetInteractionState();
            return;
        }

        SetPerimeterColor(HoverColor);
        SplitPreview.IsVisible = true;
        IndicatorLabel.IsVisible = true;
    }


    private void TrackPreview(PointerEventArgs e, SplitDirection direction) {
        var position = e.GetPosition(RootGrid);
        if (!position.HasValue) return;

        MoveIndicator(position.Value);
        MovePreview(direction, position.Value);
    }

    private void MoveIndicator(Point position) {
        IndicatorLabel.TranslationX = position.X + IndicatorOffsetX;
        IndicatorLabel.TranslationY = position.Y + IndicatorOffsetY;
    }

    private void MovePreview(SplitDirection direction, Point position) {
        SetPreviewOrientation(direction);

        if (direction.IsVerticalSplit()) {
            SplitPreview.TranslationX = position.X - PreviewHalfThickness;
            SplitPreview.TranslationY = 0;
            return;
        }

        SplitPreview.TranslationX = 0;
        SplitPreview.TranslationY = position.Y - PreviewHalfThickness;
    }

    private void SetPreviewOrientation(SplitDirection direction) {
        if (direction.IsVerticalSplit()) {
            SplitPreview.Orientation = SplitDividerVisual.DividerOrientation.Vertical;
            SplitPreview.HorizontalOptions = LayoutOptions.Start;
            SplitPreview.VerticalOptions = LayoutOptions.Fill;
            return;
        }

        SplitPreview.Orientation = SplitDividerVisual.DividerOrientation.Horizontal;
        SplitPreview.HorizontalOptions = LayoutOptions.Fill;
        SplitPreview.VerticalOptions = LayoutOptions.Start;
    }

    private void SetPerimeterColor(Color color) {
        TopArea.BackgroundColor = color;
        BottomArea.BackgroundColor = color;
        LeftArea.BackgroundColor = color;
        RightArea.BackgroundColor = color;
    }

    public void ResetInteractionState() {
        SetPerimeterColor(Colors.Transparent);

        SplitPreview.IsVisible = false;
        SplitPreview.TranslationX = 0;
        SplitPreview.TranslationY = 0;

        IndicatorLabel.IsVisible = false;
        IndicatorLabel.TranslationX = 0;
        IndicatorLabel.TranslationY = 0;
    }

}