using SimpleHID.Raw;

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

using static MU3Input.KeyboardIO;

namespace MU3Input
{
    // ReSharper disable once InconsistentNaming
    public class HidIO : IO
    {
        private HidIOConfig config;
        protected int _openCount = 0;
        private byte[] _inBuffer = new byte[64];
        private readonly SimpleRawHID _hid = new SimpleRawHID();
        private const ushort VID = 0x2341;
        private const ushort PID = 0x8036;
        protected OutputData data;
        private bool reconnecting = false;


        public HidIO(HidIOConfig config)
        {
            this.config = config;
            data = new OutputData() { Buttons = new byte[10], Aime = new Aime() { Data = new byte[18] } };
            Reconnect();
            new Thread(PollThread).Start();
        }
        public override bool IsConnected => _openCount > 0;
        public override OutputData Data => data;

        public override void Reconnect()
        {
            if (reconnecting) return;
            reconnecting = true;
            if (IsConnected)
                _hid.Close();

            _openCount = _hid.Open(1, VID, PID);
            reconnecting = false;
        }

        public static int[] bitPosMap =
        {
            23, 19, 22, 20, 21, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6
        };


        private unsafe void PollThread()
        {
            while (true)
            {
                if (!IsConnected)
                    continue;

                var len = _hid.Receive(0, ref _inBuffer, 64, 1000);
                if (len < 0)
                {
                    _openCount = 0;
                    _hid.Close();
                    continue;
                }

                OutputData temp = new OutputData();
                temp.Buttons = new ArraySegment<byte>(_inBuffer, 0, 10).ToArray();
                short lever;
                if (config.InvertLever)
                {
                    lever = (short)(-BitConverter.ToInt16(_inBuffer, 10) - 1);
                }
                else
                {
                    lever = BitConverter.ToInt16(_inBuffer, 10);
                }
                if (config.AutoCal)
                {
                    if (lever < config.LeverLeft)
                    {
                        config.LeverLeft = lever;
                        Console.WriteLine($"Set lever range: {config.LeverLeft}-{config.LeverRight}");
                    }
                    if (lever > config.LeverRight)
                    {
                        config.LeverRight = lever;
                        Console.WriteLine($"Set lever range: {config.LeverLeft}-{config.LeverRight}");
                    }
                }
                if (config.LeverRight != config.LeverLeft)
                {
                    double normLever = (lever - config.LeverLeft) / (double)(config.LeverRight - config.LeverLeft);
                    if (normLever < 0) normLever = 0;
                    if (normLever > 1) normLever = 1;
                    double leverd = -30000 + 60001 * normLever;
                    temp.Lever = ((short)leverd);
                }
                else
                {
                    temp.Lever = data.Lever;
                }
                temp.OptButtons = (OptButtons)_inBuffer[12];
                temp.Aime.Scan = _inBuffer[13];
                temp.Aime.Data = new byte[18];
                if (temp.Aime.Scan == 1)
                {
                    byte[] mifareID = new ArraySegment<byte>(_inBuffer, 14, 10).ToArray();
                    bool flag = true;
                    for (int i = 0; i < 10; i++)
                    {
                        if (mifareID[i] != 255)
                        {
                            flag = false;
                            break;
                        }
                    }
                    if (flag)
                    {
                        mifareID = Utils.ReadOrCreateAimeTxt();
                    }
                    temp.Aime.ID = mifareID;
                }
                if (temp.Aime.Scan == 2)
                {
                    temp.Aime.IDm = BitConverter.ToUInt64(_inBuffer, 14);
                    temp.Aime.PMm = BitConverter.ToUInt64(_inBuffer, 22);
                    temp.Aime.SystemCode = BitConverter.ToUInt16(_inBuffer, 30);
                }
                data = temp;
            }
        }

        public unsafe override void SetLed(uint data)
        {
            if (!IsConnected)
                return;

            SetLedInput led;
            led.Type = 0;
            led.LedBrightness = 40;

            for (var i = 0; i < 9; i++)
            {
                led.LedColors[i] = (byte)(((data >> bitPosMap[i]) & 1) * 255);
                led.LedColors[i + 15] = (byte)(((data >> bitPosMap[i + 9]) & 1) * 255);
            }

            var outBuffer = new byte[64];
            fixed (void* d = outBuffer)
                Kernel32.CopyMemory(d, &led, 64);

            _hid.Send(0, outBuffer, 64, 1000);
        }

    }
    public class HidIOConfig
    {
        public bool AutoCal { get; set; } = true;
        public short LeverLeft { get; set; } = short.MaxValue;
        public short LeverRight { get; set; } = short.MinValue;
        public bool InvertLever { get; set; } = true;
    }
}