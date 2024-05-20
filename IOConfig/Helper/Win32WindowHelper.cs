using System.Runtime.InteropServices;
using IOConfig.Common;
using Microsoft.UI.Xaml;

using static IOConfig.Common.Win32;

namespace IOConfig.Helper
{
    internal class Win32WindowHelper
    {
        private static Win32.WinProc newWndProc = null;
        private static nint oldWndProc = nint.Zero;

        private POINT? minWindowSize = null;
        private POINT? maxWindowSize = null;

        private readonly Window window;

        public Win32WindowHelper(Window window)
        {
            this.window = window;
        }

        public void SetWindowMinMaxSize(POINT? minWindowSize = null, POINT? maxWindowSize = null)
        {
            this.minWindowSize = minWindowSize;
            this.maxWindowSize = maxWindowSize;

            var hwnd = GetWindowHandleForCurrentWindow(window);

            newWndProc = new Win32.WinProc(WndProc);
            oldWndProc = SetWindowLongPtr(hwnd, Win32.WindowLongIndexFlags.GWL_WNDPROC, newWndProc);
        }

        private static nint GetWindowHandleForCurrentWindow(object target) =>
            WinRT.Interop.WindowNative.GetWindowHandle(target);

        private nint WndProc(nint hWnd, Win32.WindowMessage Msg, nint wParam, nint lParam)
        {
            switch (Msg)
            {
                case Win32.WindowMessage.WM_GETMINMAXINFO:
                    var dpi = GetDpiForWindow(hWnd);
                    var scalingFactor = (float)dpi / 96;

                    var minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                    if (minWindowSize != null)
                    {
                        minMaxInfo.ptMinTrackSize.x = (int)(minWindowSize.Value.x * scalingFactor);
                        minMaxInfo.ptMinTrackSize.y = (int)(minWindowSize.Value.y * scalingFactor);
                    }
                    if (maxWindowSize != null)
                    {
                        minMaxInfo.ptMaxTrackSize.x = (int)(maxWindowSize.Value.x * scalingFactor);
                        minMaxInfo.ptMaxTrackSize.y = (int)(minWindowSize.Value.y * scalingFactor);
                    }

                    Marshal.StructureToPtr(minMaxInfo, lParam, true);
                    break;

            }
            return CallWindowProc(oldWndProc, hWnd, Msg, wParam, lParam);
        }

        private nint SetWindowLongPtr(nint hWnd, Win32.WindowLongIndexFlags nIndex, Win32.WinProc newProc)
        {
            if (nint.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, newProc);
            else
                return new nint(SetWindowLong32(hWnd, nIndex, newProc));
        }

        internal struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }
    }
}
