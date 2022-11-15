using MU3Input;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    internal class Program
    {
        static unsafe void Main(string[] args)
        {
            ulong id = 0;
            ushort code = 0;
            IntPtr aimeid = Marshal.AllocHGlobal(10);
            Mu3IO.Init();
            AimiIO.Init();
            while (true)
            {
                Task.Delay(10).Wait();
                Mu3IO.Poll();
                Mu3IO.GetGameButtons(out byte left, out byte right);
                AimiIO.GetFelicaId(0, &id);
                AimiIO.GetFelicaPm(0, &id);
                AimiIO.GetFelicaSystemCode(0, &code);
                AimiIO.GetAimeId(0, (byte*)aimeid,10);
                Mu3IO.GetOpButtons(out byte btn);
                Debug.WriteLine(btn);
            }
            Console.ReadKey();
        }
    }
}
