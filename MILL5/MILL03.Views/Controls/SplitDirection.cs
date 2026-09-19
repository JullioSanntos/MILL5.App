using System;
using System.Collections.Generic;
using System.Text;

namespace MILL03.Views.Controls {
    public enum SplitDirection {
        Top,
        Bottom,
        Left,
        Right
    }
    public static class SplitDirectionExtensions {
        public static bool IsVerticalSplit(this SplitDirection direction) =>
            direction is SplitDirection.Top or SplitDirection.Bottom;

        public static bool IsHorizontalSplit(this SplitDirection direction) =>
            direction is SplitDirection.Left or SplitDirection.Right;
    }
}
