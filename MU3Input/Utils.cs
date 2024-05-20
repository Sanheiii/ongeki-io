using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;

namespace MU3Input
{
    internal class Utils
    {
        public static byte[] ReadOrCreateAimeTxt()
        {
            byte[] aimeId;
            var location = AppContext.BaseDirectory;
            string directoryName = Path.GetDirectoryName(location);
            string deviceDirectory = Path.Combine(directoryName, "DEVICE");
            string aimeIdPath = Path.Combine(deviceDirectory, "aime.txt");
            try
            {
                var id = BigInteger.Parse(File.ReadAllText(aimeIdPath));
                var bytes = id.ToBcd();
                aimeId = new byte[10 - bytes.Length].Concat(bytes).ToArray();
            }
            catch (Exception)
            {
                Random random = new Random();
                byte[] temp = new byte[20];
                for (var index = 0; index < temp.Length; index++)
                {
                    if (index == 0)
                    {
                        temp[index] = (byte)random.Next(0, 9);
                        if (temp[index] == 3) temp[index]++;
                    }
                    else
                    {
                        temp[index] = (byte)random.Next(0, 10);
                    }
                }
                string valueStr = string.Concat(temp.Select(b => b.ToString()).ToArray());
                var id = BigInteger.Parse(valueStr);
                if (!Directory.Exists(deviceDirectory))
                {
                    Directory.CreateDirectory(deviceDirectory);
                }
                var bytes = id.ToBcd();
                aimeId = new byte[10 - bytes.Length].Concat(bytes).ToArray();
                File.WriteAllText(aimeIdPath, id.ToString());
            }
            return aimeId;
        }

        static int currentProcessId = -1;
        public static bool IsForeground()
        {
            IntPtr foregroundWindow = User32.GetForegroundWindow();
            User32.GetWindowThreadProcessId(foregroundWindow, out int foregroundProcessId);
            if(currentProcessId == -1) currentProcessId = Process.GetCurrentProcess().Id;
            return foregroundProcessId == currentProcessId;
        }
    }
}
