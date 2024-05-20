using System.Runtime.InteropServices;
using System.Text;

namespace MU3Input
{
    public class User32
    {
        [DllImport("user32.dll", EntryPoint = "GetForegroundWindow")]
        public static extern IntPtr GetForegroundWindow();


        [DllImport("user32.dll", EntryPoint = "GetWindowText")]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int maxCount);

        [DllImport("user32.dll", EntryPoint = "GetAsyncKeyState")]
        public static extern int GetAsyncKeyState(int vKey);
        
        [DllImport("user32.dll", EntryPoint = "MapVirtualKeyEx")]
        public static extern uint MapVirtualKeyEx(uint uCode, uint uMapType, IntPtr dwhkl);

        [DllImport("user32.dll", EntryPoint = "GetWindowThreadProcessId")]
        public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
    }
}