using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace IOConfig
{
    internal static class Mu3IO
    {
        [DllImport("MU3Input.dll", EntryPoint = "mu3_io_get_api_version", CallingConvention = CallingConvention.Cdecl)]
        public static extern ushort GetVersion();

        [DllImport("MU3Input.dll", EntryPoint = "mu3_io_init", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint Init();

        [DllImport("MU3Input.dll", EntryPoint = "mu3_io_poll", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint Poll();

        [DllImport("MU3Input.dll", EntryPoint = "mu3_io_get_opbtns", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe void GetOpButtons(byte* opbtn);

        [DllImport("MU3Input.dll", EntryPoint = "mu3_io_get_gamebtns", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe void GetGameButtons(byte* left, byte* right);

        [DllImport("MU3Input.dll", EntryPoint = "mu3_io_get_lever", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe void GetLever(short* pos);

        [DllImport("MU3Input.dll", EntryPoint = "mu3_io_set_led", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetLed(uint data);

        [DllImport("MU3Input.dll", EntryPoint = "mu3_io_is_busy", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool IsBusy();

        [DllImport("MU3Input.dll", EntryPoint = "mu3_io_is_connected", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool IsConnected(int index);

    }
}
