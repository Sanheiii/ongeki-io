using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace IOConfig
{
    internal class AimeIO
    {
        [DllImport("MU3Input.dll", EntryPoint = "aime_io_get_api_version", CallingConvention = CallingConvention.Cdecl)]
        public static extern ushort GetVersion();

        [DllImport("MU3Input.dll", EntryPoint = "aime_io_init", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint Init();

        [DllImport("MU3Input.dll", EntryPoint = "aime_io_nfc_poll", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint Poll(byte unitNumber);

        [DllImport("MU3Input.dll", EntryPoint = "aime_io_nfc_get_felica_id", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe uint GetFelicaId(byte unitNumber, ulong* id);

        [DllImport("MU3Input.dll", EntryPoint = "aime_io_nfc_get_felica_pm", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe uint GetFelicaPm(byte unitNumber, ulong* pm);

        [DllImport("MU3Input.dll", EntryPoint = "aime_io_nfc_get_felica_system_code", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe uint GetFelicaSystemCode(byte unitNumber, ushort* systemCode);

        [DllImport("MU3Input.dll", EntryPoint = "aime_io_nfc_get_aime_id", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe uint GetAimeId(byte unitNumber, byte* id, ulong size);

        [DllImport("MU3Input.dll", EntryPoint = "aime_io_led_set_color", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetColor(byte unitNumber, byte r, byte g, byte b);
    }
}
