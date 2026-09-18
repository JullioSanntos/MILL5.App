using System;
using System.Collections.Generic;
using System.Text;

namespace MILL03.Views.Controls {
    public class SplitDividerVisual : Grid {
        public enum DividerOrientation { Vertical, Horizontal }

        public static readonly BindableProperty OrientationProperty = BindableProperty.Create(
            nameof(Orientation),
            typeof(DividerOrientation),
            typeof(SplitDividerVisual),
            DividerOrientation.Vertical,
            propertyChanged: OnOrientationChanged);

        public DividerOrientation Orientation {
            get => (DividerOrientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        private readonly BoxView _centerLine;

        public SplitDividerVisual() {
            BackgroundColor = Color.FromArgb("#B8B8B8");
            InputTransparent = true;

            _centerLine = new BoxView {
                Color = Color.FromArgb("#303030"),
                InputTransparent = true
            };

            Children.Add(_centerLine);
            UpdateAppearance();
        }

        private static void OnOrientationChanged(BindableObject bindable, object oldValue, object newValue) {
            ((SplitDividerVisual)bindable).UpdateAppearance();
        }

        private void UpdateAppearance() {
            if (Orientation == DividerOrientation.Vertical) {
                WidthRequest = 5;
                HeightRequest = -1;

                _centerLine.WidthRequest = 1;
                _centerLine.HeightRequest = -1;
                _centerLine.HorizontalOptions = LayoutOptions.Center;
                _centerLine.VerticalOptions = LayoutOptions.Fill;
            }
            else {
                WidthRequest = -1;
                HeightRequest = 5;

                _centerLine.WidthRequest = -1;
                _centerLine.HeightRequest = 1;
                _centerLine.HorizontalOptions = LayoutOptions.Fill;
                _centerLine.VerticalOptions = LayoutOptions.Center;
            }
        }
    }
}
