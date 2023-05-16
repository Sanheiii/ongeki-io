using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace MU3Input
{
    internal class KeyboardIO : IO
    {
        private KeyboardIOConfig config;
        public override OutputData Data => GetData();

        public override bool IsConnected => true;

        public KeyboardIO(KeyboardIOConfig param)
        {
            config = param;
        }

        public override void Reconnect() { }

        public override void SetLed(uint data) { }


        StringBuilder sb = new StringBuilder();
        private OutputData GetData()
        {
            IntPtr handle = User32.GetForegroundWindow();
            User32.GetWindowText(handle, sb, 64);
            string windowText = sb.ToString();
            if (windowText != "Otoge" && windowText != "Ongeki IO Debug" && windowText != "NAGEKI IO & 读卡器")
            {
                return new OutputData() { Buttons = new byte[10], Aime = new Aime() { Data = new byte[18] } };
            }

            byte[] buttons = new byte[] {
                Pressed(config.L1),
                Pressed(config.L2),
                Pressed(config.L3),
                Pressed(config.LSide),
                Pressed(config.LMenu),
                Pressed(config.R1),
                Pressed(config.R2),
                Pressed(config.R3),
                Pressed(config.RSide),
                Pressed(config.RMenu),
            };
            short lever = 0;
            OptButtons optButtons = (OptButtons)(Pressed(config.Test) << 0 | Pressed(config.Service) << 1 | Pressed(config.Coin) << 2);
            Aime aime = new Aime()
            {
                Scan = Pressed(config.Scan),
                Data = new byte[18]
            };
            if (aime.Scan == 1)
            {
                byte[] bytes = Utils.ReadOrCreateAimeTxt();
                aime.ID = bytes;
            }
            return new OutputData
            {
                Buttons = buttons,
                Lever = lever,
                OptButtons = optButtons,
                Aime = aime
            };
        }
        private byte Pressed(Keys key)
        {

            return User32.GetAsyncKeyState(key) == 0 ? (byte)0 : (byte)1;
        }
        public class KeyboardIOConfig
        {
            public Keys L1 { get; set; } = (Keys)(-1);
            public Keys L2 { get; set; } = (Keys)(-1);
            public Keys L3 { get; set; } = (Keys)(-1);
            public Keys LSide { get; set; } = (Keys)(-1);
            public Keys LMenu { get; set; } = (Keys)(-1);
            public Keys R1 { get; set; } = (Keys)(-1);
            public Keys R2 { get; set; } = (Keys)(-1);
            public Keys R3 { get; set; } = (Keys)(-1);
            public Keys RSide { get; set; } = (Keys)(-1);
            public Keys RMenu { get; set; } = (Keys)(-1);
            public Keys Test { get; set; } = (Keys)(-1);
            public Keys Service { get; set; } = (Keys)(-1);
            public Keys Coin { get; set; } = (Keys)(-1);
            public Keys Scan { get; set; } = (Keys)(-1);
        }
    }
}
