using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MU3Input
{
    public static class AimiIO
    {
        [DllExport(CallingConvention = CallingConvention.Cdecl, ExportName = "aime_io_get_api_version")]
        public static ushort GetVersion() => 0x0200;

        [DllExport(CallingConvention = CallingConvention.Cdecl, ExportName = "aime_io_init")]
        public static uint Init()
        {
            if (Process.GetCurrentProcess().ProcessName != "amdaemon" &&
                Process.GetCurrentProcess().ProcessName != "Debug" &&
                Process.GetCurrentProcess().ProcessName != "Test")
                return 1;

            return 0;
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl, ExportName = "aime_io_nfc_poll")]
        public static uint Poll(byte unitNumber)
        {
            return 0;
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl, ExportName = "aime_io_nfc_get_felica_id")]
        public static unsafe uint GetFelicaId(byte unitNumber, ulong* id)
        {
            if (Mu3IO.IO == null || Mu3IO.IO.Aime.Scan != 2)
            {
                return 1;
            }
            else
            {
                *id = Mu3IO.IO.Aime.IDm;
                return 0;
            }
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl, ExportName = "aime_io_nfc_get_felica_pm")]
        public static unsafe uint GetFelicaPm(byte unitNumber, ulong* pm)
        {
            if (Mu3IO.IO == null || Mu3IO.IO.Aime.Scan != 2)
            {
                return 1;
            }
            else
            {
                *pm = Mu3IO.IO.Aime.PMm;
                return 0;
            }
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl, ExportName = "aime_io_nfc_get_felica_system_code")]
        public static unsafe uint GetFelicaSystemCode(byte unitNumber, ushort* systemCode)
        {
            if (Mu3IO.IO == null || Mu3IO.IO.Aime.Scan != 2)
            {
                return 1;
            }
            else
            {
                *systemCode = Mu3IO.IO.Aime.SystemCode;
                return 0;
            }
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl, ExportName = "aime_io_nfc_get_aime_id")]
        public static unsafe uint GetAimeId(byte unitNumber, byte* id, ulong size)
        {
            if (Mu3IO.IO == null || Mu3IO.IO.Aime.Scan != 1) return 1;
            Aime aime = Mu3IO.IO.Aime;
            for(int i = 0; i < 10; i++)
            {
                id[i]=aime.ID[i];
            }

            return 0;
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl, ExportName = "aime_io_led_set_color")]
        public static void SetColor(byte unitNumber, byte r, byte g, byte b)
        {

        }
    }
}
