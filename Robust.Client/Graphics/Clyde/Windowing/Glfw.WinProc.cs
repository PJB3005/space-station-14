using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenToolkit.GraphicsLibraryFramework;
using Robust.Shared.Log;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.GWL;
using static TerraFX.Interop.Windows.Windows;
using static TerraFX.Interop.Windows.WM;

namespace Robust.Client.Graphics.Clyde;

internal partial class Clyde
{
    private sealed unsafe partial class GlfwWindowingImpl
    {
        //
        // Custom window procedure stuff to hook some Windows messages that GLFW doesn't handle.
        // Yes this causes non-Windows platforms to get the short straw,
        // but I'm kinda just waiting on SDL2 to get DPI awareness on Windows so I can drop GLFW like a rock.
        //

        // I hate to do this but it's not like GLFW can be ran twice anyways.
        private static GlfwWindowingImpl? _implStatic;

        private readonly Dictionary<HWND, HWndData> _hWndData = new();

        private void HookWndProc(Window* window)
        {
            if (!OperatingSystem.IsWindows())
                return;

            var hWnd = (HWND) GLFW.GetWin32Window(window);

            delegate* unmanaged<HWND, uint, WPARAM, LPARAM, LRESULT> proc = &WndProc;
            var old = (delegate* unmanaged<HWND, uint, WPARAM, LPARAM, LRESULT>)
                SetWindowLongPtrW(hWnd, GWL_WNDPROC, (nint)proc);

            _hWndData.Add(hWnd, new HWndData
            {
                Window = window,
                OldWindowProc = old
            });
        }

        private void WndProcCloseWindow(Window* window)
        {
            if (!OperatingSystem.IsWindows())
                return;

            var hWnd = (HWND) GLFW.GetWin32Window(window);
            var oldFunc = _hWndData[hWnd].OldWindowProc;
            // Allow GLFW to handle any messages raised when the window is closed itself, we don't care anymore.
            SetWindowLongPtrW(hWnd, GWL_WNDPROC, (nint)oldFunc);
            _hWndData.Remove(hWnd);
        }

        [UnmanagedCallersOnly]
        private static LRESULT WndProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam)
        {
            if (_implStatic == null || !_implStatic._hWndData.TryGetValue(hWnd, out var data))
            {
                Logger.ErrorS("clyde.win", "Unable to find window in WndProc!");
                return DefWindowProcW(hWnd, uMsg, wParam, lParam);
            }

            var ret = CallWindowProcW(data.OldWindowProc, hWnd, uMsg, wParam, lParam);

            switch (uMsg)
            {
                case WM_INPUTLANGCHANGE:
                {
                    // GLFW uses this to update its internal key map cache,
                    // but doesn't have any sort of event for it.
                    _implStatic.OnKeyboardModeChange(data.Window);
                    break;
                }
            }

            return ret;
        }

        private struct HWndData
        {
            public Window* Window;
            public delegate* unmanaged<HWND, uint, WPARAM, LPARAM, LRESULT> OldWindowProc;
        }
    }
}
