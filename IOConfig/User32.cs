using System;
using System.Runtime.InteropServices;
using System.Text;

namespace IOConfig
{
    public class User32
    {
        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetForegroundWindow")]
        public static extern IntPtr GetForegroundWindow();


        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetWindowText")]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int maxCount);

        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetAsyncKeyState")]
        public static extern int GetAsyncKeyState(int vKey);
        
        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "MapVirtualKeyEx")]
        public static extern uint MapVirtualKeyEx(uint uCode, uint uMapType, IntPtr dwhkl);
    }
}