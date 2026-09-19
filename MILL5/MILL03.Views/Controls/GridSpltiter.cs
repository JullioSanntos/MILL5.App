using Microsoft.Maui.Controls;
using System;

#if WINDOWS
using Microsoft.UI.Xaml.Input;
using WinUIElement = Microsoft.UI.Xaml.UIElement;
using WinUIPointer = Microsoft.UI.Xaml.Input.Pointer;
#endif

namespace MILL03.Views.Controls;

public class GridSplitter : Grid {
    public enum SplitOrientation { Vertical, Horizontal }

    private const double RestingThickness = 3;
    private const double ActiveThickness = 5;

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

#if WINDOWS

    private WinUIElement? _platformView;
    private WinUIPointer? _capturedPointer;

    private Grid? _dragParentGrid;
    private WinUIElement? _dragParentPlatformView;
    private int _dragIndex;
    private double _dragStartPosition;

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

#if WINDOWS

        HandlerChanged += OnHandlerChanged;

#else

        var panGesture = new PanGestureRecognizer();
        panGesture.PanUpdated += OnPanUpdated;
        GestureRecognizers.Add(panGesture);

#endif
    }

    #region Appearance

    private static void OnOrientationChanged(
        BindableObject bindable,
        object oldValue,
        object newValue) {

        ((GridSplitter)bindable).UpdateAppearance();
    }

    private void UpdateAppearance() {
        bool isVertical = Orientation == SplitOrientation.Vertical;

        _dividerVisual.Orientation = isVertical
            ? SplitDividerVisual.DividerOrientation.Vertical
            : SplitDividerVisual.DividerOrientation.Horizontal;

        _dividerVisual.WidthRequest = isVertical
            ? RestingThickness
            : -1;

        _dividerVisual.HeightRequest = isVertical
            ? -1
            : RestingThickness;

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

    #endregion Appearance

#if WINDOWS

    #region Windows Pointer Interaction

    private void OnHandlerChanged(object? sender, EventArgs e) {
        UnwirePlatformView();

        _platformView = Handler?.PlatformView as WinUIElement;
        if (_platformView == null) return;

        _platformView.PointerEntered += OnPointerEntered;
        _platformView.PointerExited += OnPointerExited;
        _platformView.PointerPressed += OnPointerPressed;
        _platformView.PointerMoved += OnPointerMoved;
        _platformView.PointerReleased += OnPointerReleased;
        _platformView.PointerCanceled += OnPointerCanceled;
        _platformView.PointerCaptureLost += OnPointerCaptureLost;
    }

    private void UnwirePlatformView() {
        if (_platformView == null) return;

        _platformView.PointerEntered -= OnPointerEntered;
        _platformView.PointerExited -= OnPointerExited;
        _platformView.PointerPressed -= OnPointerPressed;
        _platformView.PointerMoved -= OnPointerMoved;
        _platformView.PointerReleased -= OnPointerReleased;
        _platformView.PointerCanceled -= OnPointerCanceled;
        _platformView.PointerCaptureLost -= OnPointerCaptureLost;

        _platformView = null;
    }

    private void OnPointerEntered(
        object sender,
        PointerRoutedEventArgs e) {

        if (_isDragging) return;

        SetInteractionThickness(ActiveThickness);
        SetResizeCursor();
    }

    private void OnPointerExited(
        object sender,
        PointerRoutedEventArgs e) {

        if (_isDragging || _capturedPointer != null) return;

        SetInteractionThickness(RestingThickness);
        ResetCursor();
    }

    private void OnPointerPressed(
        object sender,
        PointerRoutedEventArgs e) {

        if (_platformView == null) return;

        var point = e.GetCurrentPoint(_platformView);
        if (!point.Properties.IsLeftButtonPressed) return;

        SetInteractionThickness(ActiveThickness);
        SetResizeCursor();

        if (!_platformView.CapturePointer(e.Pointer)) {
            System.Diagnostics.Debug.WriteLine(
                "GridSplitter: CapturePointer failed.");

            SetInteractionThickness(RestingThickness);
            ResetCursor();
            return;
        }

        _capturedPointer = e.Pointer;

        if (!BeginDrag(e)) {
            ReleaseCapturedPointer();
            EndDrag();
            return;
        }

        _isDragging = true;

        // No MAUI PanGestureRecognizer is involved on Windows,
        // so the native pointer interaction owns this event.
        e.Handled = true;
    }

    private bool BeginDrag(PointerRoutedEventArgs e) {
        if (Parent is not Grid parentGrid) return false;

        bool isVertical = Orientation == SplitOrientation.Vertical;
        int index = isVertical
            ? Grid.GetColumn(this)
            : Grid.GetRow(this);

        if (isVertical) {
            if (index <= 0 ||
                index >= parentGrid.ColumnDefinitions.Count - 1) {
                return false;
            }

            _initialStar1 =
                parentGrid.ColumnDefinitions[index - 1].Width.Value;

            _initialStar2 =
                parentGrid.ColumnDefinitions[index + 1].Width.Value;
        }
        else {
            if (index <= 0 ||
                index >= parentGrid.RowDefinitions.Count - 1) {
                return false;
            }

            _initialStar1 =
                parentGrid.RowDefinitions[index - 1].Height.Value;

            _initialStar2 =
                parentGrid.RowDefinitions[index + 1].Height.Value;
        }

        var parentPlatformView =
            parentGrid.Handler?.PlatformView as WinUIElement;

        if (parentPlatformView == null) return false;

        var position =
            e.GetCurrentPoint(parentPlatformView).Position;

        _dragParentGrid = parentGrid;
        _dragParentPlatformView = parentPlatformView;
        _dragIndex = index;

        _dragStartPosition = isVertical
            ? position.X
            : position.Y;

        return true;
    }

    private void OnPointerMoved(
        object sender,
        PointerRoutedEventArgs e) {

        if (!_isDragging ||
            _capturedPointer == null ||
            e.Pointer.PointerId != _capturedPointer.PointerId) {
            return;
        }

        ResizeFromPointer(e);
        e.Handled = true;
    }

    private void ResizeFromPointer(PointerRoutedEventArgs e) {
        if (_dragParentGrid == null ||
            _dragParentPlatformView == null) {
            return;
        }

        bool isVertical = Orientation == SplitOrientation.Vertical;

        var position =
            e.GetCurrentPoint(_dragParentPlatformView).Position;

        double currentPosition = isVertical
            ? position.X
            : position.Y;

        double pixelDelta =
            currentPosition - _dragStartPosition;

        double gridSize = isVertical
            ? _dragParentGrid.Width
            : _dragParentGrid.Height;

        if (gridSize <= 0) return;

        double percentMove = pixelDelta / gridSize;

        double starDelta =
            percentMove * (_initialStar1 + _initialStar2);

        double newStar1 =
            Math.Max(0.01, _initialStar1 + starDelta);

        double newStar2 =
            Math.Max(0.01, _initialStar2 - starDelta);

        if (isVertical) {
            _dragParentGrid
                .ColumnDefinitions[_dragIndex - 1]
                .Width =
                    new GridLength(newStar1, GridUnitType.Star);

            _dragParentGrid
                .ColumnDefinitions[_dragIndex + 1]
                .Width =
                    new GridLength(newStar2, GridUnitType.Star);
        }
        else {
            _dragParentGrid
                .RowDefinitions[_dragIndex - 1]
                .Height =
                    new GridLength(newStar1, GridUnitType.Star);

            _dragParentGrid
                .RowDefinitions[_dragIndex + 1]
                .Height =
                    new GridLength(newStar2, GridUnitType.Star);
        }
    }

    private void OnPointerReleased(
        object sender,
        PointerRoutedEventArgs e) {

        if (_capturedPointer != null &&
            e.Pointer.PointerId == _capturedPointer.PointerId) {

            e.Handled = true;
        }

        ReleaseCapturedPointer();
        EndDrag();
    }

    private void OnPointerCanceled(
        object sender,
        PointerRoutedEventArgs e) {

        ReleaseCapturedPointer();
        EndDrag();
    }

    private void OnPointerCaptureLost(
        object sender,
        PointerRoutedEventArgs e) {

        _capturedPointer = null;
        EndDrag();
    }

    private void ReleaseCapturedPointer() {
        if (_platformView == null ||
            _capturedPointer == null) {
            return;
        }

        var pointer = _capturedPointer;
        _capturedPointer = null;

        _platformView.ReleasePointerCapture(pointer);
    }

    private void EndDrag() {
        _isDragging = false;

        _dragParentGrid = null;
        _dragParentPlatformView = null;
        _dragIndex = 0;
        _dragStartPosition = 0;

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

    private void ResetCursor() {
        _platformView?.ForceSetCursor(
            Microsoft.UI.Input.InputSystemCursorShape.Arrow);
    }

    #endregion Windows Pointer Interaction

#else

    #region MAUI Pan Interaction

    private void OnPanUpdated(
        object? sender,
        PanUpdatedEventArgs e) {

        if (Parent is not Grid parentGrid) return;

        bool isVertical =
            Orientation == SplitOrientation.Vertical;

        int index = isVertical
            ? Grid.GetColumn(this)
            : Grid.GetRow(this);

        if (isVertical &&
            (index <= 0 ||
             index >= parentGrid.ColumnDefinitions.Count - 1)) {
            return;
        }

        if (!isVertical &&
            (index <= 0 ||
             index >= parentGrid.RowDefinitions.Count - 1)) {
            return;
        }

        if (e.StatusType == GestureStatus.Started) {
            _isDragging = true;

            if (isVertical) {
                _initialStar1 =
                    parentGrid.ColumnDefinitions[index - 1]
                        .Width.Value;

                _initialStar2 =
                    parentGrid.ColumnDefinitions[index + 1]
                        .Width.Value;
            }
            else {
                _initialStar1 =
                    parentGrid.RowDefinitions[index - 1]
                        .Height.Value;

                _initialStar2 =
                    parentGrid.RowDefinitions[index + 1]
                        .Height.Value;
            }
        }
        else if (e.StatusType == GestureStatus.Running) {
            if (isVertical) {
                double gridWidth = parentGrid.Width;
                if (gridWidth <= 0) return;

                double percentMove =
                    e.TotalX / gridWidth;

                double starDelta =
                    percentMove *
                    (_initialStar1 + _initialStar2);

                double newStar1 =
                    Math.Max(
                        0.01,
                        _initialStar1 + starDelta);

                double newStar2 =
                    Math.Max(
                        0.01,
                        _initialStar2 - starDelta);

                parentGrid
                    .ColumnDefinitions[index - 1]
                    .Width =
                        new GridLength(
                            newStar1,
                            GridUnitType.Star);

                parentGrid
                    .ColumnDefinitions[index + 1]
                    .Width =
                        new GridLength(
                            newStar2,
                            GridUnitType.Star);
            }
            else {
                double gridHeight = parentGrid.Height;
                if (gridHeight <= 0) return;

                double percentMove =
                    e.TotalY / gridHeight;

                double starDelta =
                    percentMove *
                    (_initialStar1 + _initialStar2);

                double newStar1 =
                    Math.Max(
                        0.01,
                        _initialStar1 + starDelta);

                double newStar2 =
                    Math.Max(
                        0.01,
                        _initialStar2 - starDelta);

                parentGrid
                    .RowDefinitions[index - 1]
                    .Height =
                        new GridLength(
                            newStar1,
                            GridUnitType.Star);

                parentGrid
                    .RowDefinitions[index + 1]
                    .Height =
                        new GridLength(
                            newStar2,
                            GridUnitType.Star);
            }
        }
        else if (e.StatusType == GestureStatus.Completed ||
                 e.StatusType == GestureStatus.Canceled) {

            _isDragging = false;
        }
    }

    #endregion MAUI Pan Interaction

#endif
}