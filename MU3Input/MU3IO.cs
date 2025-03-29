using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MU3Input
{
    public static class Mu3IO
    {
        private static bool isBusy;
        private static readonly System.Timers.Timer loadTimer;
        private static readonly FileSystemWatcher watcher;
        internal static MixedIO IO;

        public static uint InitIO()
        {
            var old = IO;
            IO = new MixedIO();
            old?.Dispose();
            var config = Config.Load();
            if (config is null) return 1;
            foreach (var ioConfig in config.IO)
            {
                IO.Add(IO.CreateIO(ioConfig.Type, ioConfig.Param), ioConfig.Scope);
            }
            return 0;
        }

#if RELEASE
        [UnmanagedCallersOnly(EntryPoint = "mu3_io_get_api_version", CallConvs = [typeof(CallConvCdecl)])]
#endif
        public static ushort GetVersion()
        {
            return 0x0102;
        }

#if RELEASE
        [UnmanagedCallersOnly(EntryPoint = "mu3_io_init", CallConvs = [typeof(CallConvCdecl)])]
#endif
        public static uint Init()
        {
            isBusy = true;
            var result = InitIO();
            isBusy = false;
            return result;
        }

#if RELEASE
        [UnmanagedCallersOnly(EntryPoint = "mu3_io_poll", CallConvs = [typeof(CallConvCdecl)])]
#endif
        public static uint Poll()
        {
            return 0;
        }

#if RELEASE
        [UnmanagedCallersOnly(EntryPoint = "mu3_io_get_opbtns", CallConvs = [typeof(CallConvCdecl)])]
#endif
        public static unsafe void GetOpButtons(byte *opbtn)
        {
            *opbtn = (byte)IO.OptButtonsStatus;
        }

#if RELEASE
        [UnmanagedCallersOnly(EntryPoint = "mu3_io_get_gamebtns", CallConvs = [typeof(CallConvCdecl)])]
#endif
        public static unsafe void GetGameButtons(byte *left, byte *right)
        {
            *left = IO.LeftButton;
            *right = IO.RightButton;
        }

#if RELEASE
        [UnmanagedCallersOnly(EntryPoint = "mu3_io_get_lever", CallConvs = [typeof(CallConvCdecl)])]
#endif
        public static unsafe void GetLever(short *pos)
        {
            *pos = IO.Lever;
        }

#if RELEASE
        [UnmanagedCallersOnly(EntryPoint = "mu3_io_led_init", CallConvs = [typeof(CallConvCdecl)])]
#endif
        public static unsafe uint LedInit()
        {
            return 0;
        }

#if RELEASE
        [UnmanagedCallersOnly(EntryPoint = "mu3_io_led_set_colors", CallConvs = [typeof(CallConvCdecl)])]
#endif
        public static unsafe void SetLed(byte board, byte* data)
        {
            if(board == 1)
            {
                byte[] buffer = new byte[18];
                Marshal.Copy((IntPtr)data, buffer, 0, 18);
                IO.SetLed(buffer);
            }
        }

#if RELEASE
        [UnmanagedCallersOnly(EntryPoint = "mu3_io_is_busy", CallConvs = [typeof(CallConvCdecl)])]
#endif
        public static bool IsBusy()
        {
            return isBusy;
        }

#if RELEASE
        [UnmanagedCallersOnly(EntryPoint = "mu3_io_is_connected", CallConvs = [typeof(CallConvCdecl)])]
#endif
        public static bool IsConnected(int index)
        {
            return IO.Items.ElementAtOrDefault(index).Key?.IsConnected ?? false;
        }

    }
}
