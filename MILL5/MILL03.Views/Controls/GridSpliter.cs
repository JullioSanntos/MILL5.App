using Microsoft.Maui.Controls;
using System;

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

    private double _initialStar1;
    private double _initialStar2;
    private bool _isDragging;

    public GridSplitter() {
        // Apply styling directly to the BoxView to guarantee hit-testing
        //BackgroundColor = Color.FromArgb("#80808080");
        BackgroundColor = Colors.Orange;
        ZIndex = 100;

        var panGesture = new PanGestureRecognizer();
        panGesture.PanUpdated += OnPanUpdated;
        GestureRecognizers.Add(panGesture);

        this.HandlerChanged += OnHandlerChanged;
    }

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e) {
        if (Parent is not Grid parentGrid) return;

        bool isVertical = Orientation == SplitOrientation.Vertical;
        int index = isVertical ? Grid.GetColumn(this) : Grid.GetRow(this);

        if (isVertical && (index <= 0 || index >= parentGrid.ColumnDefinitions.Count - 1)) return;
        if (!isVertical && (index <= 0 || index >= parentGrid.RowDefinitions.Count - 1)) return;

        if (e.StatusType == GestureStatus.Started) {
            _isDragging = true;

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

                parentGrid.ColumnDefinitions[index - 1].Width = new GridLength(newStar1, GridUnitType.Star);
                parentGrid.ColumnDefinitions[index + 1].Width = new GridLength(newStar2, GridUnitType.Star);
            }
            else {
                double gridHeight = parentGrid.Height;
                if (gridHeight <= 0) return;

                double percentMove = e.TotalY / gridHeight;
                double starDelta = percentMove * (_initialStar1 + _initialStar2);

                double newStar1 = Math.Max(0.01, _initialStar1 + starDelta);
                double newStar2 = Math.Max(0.01, _initialStar2 - starDelta);

                parentGrid.RowDefinitions[index - 1].Height = new GridLength(newStar1, GridUnitType.Star);
                parentGrid.RowDefinitions[index + 1].Height = new GridLength(newStar2, GridUnitType.Star);
            }
        }
        else if (e.StatusType == GestureStatus.Completed || e.StatusType == GestureStatus.Canceled) {
            _isDragging = false;
            ResetCursor();
        }
    }

    private void OnHandlerChanged(object? sender, EventArgs e) {
#if WINDOWS
        if (this.Handler?.PlatformView is Microsoft.UI.Xaml.UIElement platformView) {
            platformView.PointerEntered += (s, args) => {
                var shape = this.Orientation == SplitOrientation.Vertical
                    ? Microsoft.UI.Input.InputSystemCursorShape.SizeWestEast
                    : Microsoft.UI.Input.InputSystemCursorShape.SizeNorthSouth;

                platformView.ForceSetCursor(shape);
            };

            platformView.PointerExited += (s, args) => {
                if (!_isDragging) ResetCursor();
            };
        }
#endif
    }

    private void ResetCursor() {
#if WINDOWS
        if (this.Handler?.PlatformView is Microsoft.UI.Xaml.UIElement platformView) {
            platformView.ForceSetCursor(Microsoft.UI.Input.InputSystemCursorShape.Arrow);
        }
#endif
    }
}