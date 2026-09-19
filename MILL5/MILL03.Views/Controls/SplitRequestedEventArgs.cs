using System;
using System.Collections.Generic;
using System.Text;

namespace MILL03.Views.Controls {
    public sealed class SplitRequestedEventArgs(SplitDirection direction, Point position) : EventArgs {
        public SplitDirection Direction { get; } = direction;
        public Point Position { get; } = position;
    }
}
