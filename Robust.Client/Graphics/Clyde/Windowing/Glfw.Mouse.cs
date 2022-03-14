using OpenToolkit.GraphicsLibraryFramework;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using TerraFX.Interop.Windows;

namespace Robust.Client.Graphics.Clyde;

internal partial class Clyde
{
    private sealed unsafe partial class GlfwWindowingImpl
    {
        private ExtendedMousePosData CalcExtendedMousePosData(Window* window, double x, double y)
        {
            ExtendedMousePosData data = default;
            data.SourceWindow = window;
            data.SourcePos = new Vector2((float) x, (float) y);

            if (OperatingSystem.IsWindows())
            {
                var (dstWin, vec) = CalcExtMouseDataWindows(window, x, y);
                data.OverWindow = dstWin;
                data.OverPos = vec;
            }

            return data;
        }

        private (Ptr<Window>, Vector2) CalcExtMouseDataWindows(Window* window, double x, double y)
        {
            var hWnd = (HWND) GLFW.GetWin32Window(window);
            var point = new POINT((int) x, (int) y);
            // Microsoft's docs don't say when this could ever fail so I'm just gonna uhhh return early.
            if (!Windows.ClientToScreen(hWnd, &point))
                return default;

            var foundWindow = Windows.WindowFromPoint(point);
            if (foundWindow == HWND.NULL || !_hWndData.TryGetValue(foundWindow, out var data))
                return default;

            if (!Windows.ScreenToClient(foundWindow, &point))
                return default;

            Ptr<Window> glfwWindow = data.Window;
            return (glfwWindow, new Vector2(point.x, point.y));
        }

        private struct ExtendedMousePosData
        {
            // Window that triggered mouse event and the position of the mouse in it.
            public Window* SourceWindow;
            public Vector2 SourcePos;

            // Actual window and relative position underneath the mouse.
            public Window* OverWindow;
            public Vector2 OverPos;
        }
    }
}
