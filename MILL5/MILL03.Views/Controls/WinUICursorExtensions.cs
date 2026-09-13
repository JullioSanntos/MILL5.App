#if WINDOWS
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using System.Reflection;

namespace MILL03.Views.Controls;

public static class WinUICursorExtensions {
    public static void ForceSetCursor(this UIElement element, InputSystemCursorShape shape) {
        var cursor = InputSystemCursor.Create(shape);

        typeof(UIElement).InvokeMember(
            "ProtectedCursor",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.Instance,
            null,
            element,
            new object[] { cursor });
    }
}
#endif