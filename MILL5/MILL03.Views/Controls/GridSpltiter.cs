using Microsoft.Maui.Controls;
using System;

namespace MILL03.Views.Controls;

public class GridSplitter : Grid {
    public enum SplitOrientation { Vertical, Horizontal }

    private const double RestingThickness = 3;
    private const double ActiveThickness = 7;

    public static readonly BindableProperty OrientationProperty = BindableProperty.Create(
        nameof(Orientation),
        typeof(SplitOrientation),
        typeof(GridSplitter),
        SplitOrientation.Vertical,
        propertyChanged: OnOrientationChanged);

    public SplitOrientation Orientation {
        get => (SplitOrientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    private readonly SplitDividerVisual _dividerVisual;
    private double _initialStar1;
    private double _initialStar2;
    private bool _isDragging;
    private bool _isPointerDown;

#if WINDOWS
    private Microsoft.UI.Xaml.UIElement? _platformView;
    private Microsoft.UI.Xaml.Input.Pointer? _capturedPointer;
#endif

    public GridSplitter() {
        BackgroundColor = Colors.Transparent;
        ZIndex = 100;

        _dividerVisual = new SplitDividerVisual {
            Opacity = 1.0,
            InputTransparent = true
        };

        Children.Add(_dividerVisual);
        UpdateAppearance();

        var panGesture = new PanGestureRecognizer();
        panGesture.PanUpdated += OnPanUpdated;
        GestureRecognizers.Add(panGesture);

        HandlerChanged += OnHandlerChanged;
    }

    private static void OnOrientationChanged(BindableObject bindable, object oldValue, object newValue) {
        ((GridSplitter)bindable).UpdateAppearance();
    }

    private void UpdateAppearance() {
        bool isVertical = Orientation == SplitOrientation.Vertical;

        _dividerVisual.Orientation = isVertical
            ? SplitDividerVisual.DividerOrientation.Vertical
            : SplitDividerVisual.DividerOrientation.Horizontal;

        // The committed divider is visually 3 pixels wide/high.
        _dividerVisual.WidthRequest = isVertical ? RestingThickness : -1;
        _dividerVisual.HeightRequest = isVertical ? -1 : RestingThickness;

        // Keep the 3-pixel visual centered when the hit area expands to 7.
        _dividerVisual.HorizontalOptions = isVertical
            ? LayoutOptions.Center
            : LayoutOptions.Fill;

        _dividerVisual.VerticalOptions = isVertical
            ? LayoutOptions.Fill
            : LayoutOptions.Center;

        SetInteractionThickness(RestingThickness);
    }

    private void SetInteractionThickness(double thickness) {
        if (Orientation == SplitOrientation.Vertical) {
            WidthRequest = thickness;
            HeightRequest = -1;
        }
        else {
            WidthRequest = -1;
            HeightRequest = thickness;
        }
    }

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e) {
        if (Parent is not Grid parentGrid) return;

        bool isVertical = Orientation == SplitOrientation.Vertical;
        int index = isVertical ? Grid.GetColumn(this) : Grid.GetRow(this);

        if (isVertical && (index <= 0 || index >= parentGrid.ColumnDefinitions.Count - 1)) return;
        if (!isVertical && (index <= 0 || index >= parentGrid.RowDefinitions.Count - 1)) return;

        if (e.StatusType == GestureStatus.Started) {
            _isDragging = true;
            SetInteractionThickness(ActiveThickness);

            if (isVertical) {
                _initialStar1 = parentGrid.ColumnDefinitions[index - 1].Width.Value;
                _initialStar2 = parentGrid.ColumnDefinitions[index + 1].Width.Value;
            }
            else {
                _initialStar1 = parentGrid.RowDefinitions[index - 1].Height.Value;
                _initialStar2 = parentGrid.RowDefinitions[index + 1].Height.Value;
            }
        }
        else if (e.StatusType == GestureStatus.Running) {
            if (isVertical) {
                double gridWidth = parentGrid.Width;
                if (gridWidth <= 0) return;

                double percentMove = e.TotalX / gridWidth;
                double starDelta = percentMove * (_initialStar1 + _initialStar2);

                double newStar1 = Math.Max(0.01, _initialStar1 + starDelta);
                double newStar2 = Math.Max(0.01, _initialStar2 - starDelta);

                parentGrid.ColumnDefinitions[index - 1].Width =
                    new GridLength(newStar1, GridUnitType.Star);

                parentGrid.ColumnDefinitions[index + 1].Width =
                    new GridLength(newStar2, GridUnitType.Star);
            }
            else {
                double gridHeight = parentGrid.Height;
                if (gridHeight <= 0) return;

                double percentMove = e.TotalY / gridHeight;
                double starDelta = percentMove * (_initialStar1 + _initialStar2);

                double newStar1 = Math.Max(0.01, _initialStar1 + starDelta);
                double newStar2 = Math.Max(0.01, _initialStar2 - starDelta);

                parentGrid.RowDefinitions[index - 1].Height =
                    new GridLength(newStar1, GridUnitType.Star);

                parentGrid.RowDefinitions[index + 1].Height =
                    new GridLength(newStar2, GridUnitType.Star);
            }
        }
        else if (e.StatusType == GestureStatus.Completed ||
                 e.StatusType == GestureStatus.Canceled) {

            _isDragging = false;

            // On Windows, PointerReleased / PointerCaptureLost owns
            // the transition back to the resting state.
            if (!_isPointerDown) {
                SetInteractionThickness(RestingThickness);
                ResetCursor();
            }
        }
    }

    private void OnHandlerChanged(object? sender, EventArgs e) {
#if WINDOWS
        if (_platformView != null) {
            _platformView.PointerEntered -= OnPointerEntered;
            _platformView.PointerExited -= OnPointerExited;
            _platformView.PointerPressed -= OnPointerPressed;
            _platformView.PointerReleased -= OnPointerReleased;
            _platformView.PointerCanceled -= OnPointerCanceled;
            _platformView.PointerCaptureLost -= OnPointerCaptureLost;
        }

        _platformView = Handler?.PlatformView as Microsoft.UI.Xaml.UIElement;
        if (_platformView == null) return;

        _platformView.PointerEntered += OnPointerEntered;
        _platformView.PointerExited += OnPointerExited;
        _platformView.PointerPressed += OnPointerPressed;
        _platformView.PointerReleased += OnPointerReleased;
        _platformView.PointerCanceled += OnPointerCanceled;
        _platformView.PointerCaptureLost += OnPointerCaptureLost;
#endif
    }

#if WINDOWS

    private void OnPointerEntered(
        object sender,
        Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) {

        SetInteractionThickness(ActiveThickness);
        SetResizeCursor();
    }

    private void OnPointerExited(
        object sender,
        Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) {

        if (_isPointerDown || _isDragging || _capturedPointer != null) return;

        SetInteractionThickness(RestingThickness);
        ResetCursor();
    }

    private void OnPointerPressed(
        object sender,
        Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) {

        if (_platformView == null) return;

        _isPointerDown = true;
        SetInteractionThickness(ActiveThickness);

        if (_platformView.CapturePointer(e.Pointer)) {
            _capturedPointer = e.Pointer;
        }

        // Deliberately do NOT set e.Handled.
        // MAUI's PanGestureRecognizer still needs this input.
    }

    private void OnPointerReleased(
        object sender,
        Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) {

        ReleaseCapturedPointer();
        EndPointerInteraction();
    }

    private void OnPointerCanceled(
        object sender,
        Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) {

        ReleaseCapturedPointer();
        EndPointerInteraction();
    }

    private void OnPointerCaptureLost(
        object sender,
        Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) {

        _capturedPointer = null;
        EndPointerInteraction();
    }

    private void ReleaseCapturedPointer() {
        if (_platformView == null || _capturedPointer == null) return;

        var pointer = _capturedPointer;
        _capturedPointer = null;

        _platformView.ReleasePointerCapture(pointer);
    }

    private void EndPointerInteraction() {
        _isPointerDown = false;
        _isDragging = false;

        SetInteractionThickness(RestingThickness);
        ResetCursor();
    }

    private void SetResizeCursor() {
        if (_platformView == null) return;

        var shape = Orientation == SplitOrientation.Vertical
            ? Microsoft.UI.Input.InputSystemCursorShape.SizeWestEast
            : Microsoft.UI.Input.InputSystemCursorShape.SizeNorthSouth;

        _platformView.ForceSetCursor(shape);
    }

#endif

    private void ResetCursor() {
#if WINDOWS
        _platformView?.ForceSetCursor(
            Microsoft.UI.Input.InputSystemCursorShape.Arrow);
#endif
    }
}