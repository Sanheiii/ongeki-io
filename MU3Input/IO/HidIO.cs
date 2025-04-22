using SimpleHID.Raw;

using System.Text.Json.Serialization;

namespace MU3Input
{
    public class HidIO : IO
    {
        private HidIOConfig config;
        protected int _openCount = 0;
        private byte[] _inBuffer = new byte[64];
        private SimpleRawHID? _hid = new SimpleRawHID();
        protected OutputData data;
        private bool reconnecting = false;
        bool _disposedValue = false;


        public HidIO(HidIOConfig param)
        {
            config = param;
            data = new OutputData() { Buttons = new byte[10], Aime = new Aime() { Data = new byte[18] } };
            Reconnect();
        }
        public override bool IsConnected => _hid != null && _openCount > 0;
        public override OutputData Data => data;

        public override void Reconnect()
        {
            if (reconnecting || _disposedValue || _hid == null) return;
            reconnecting = true;
            if (IsConnected)
                _hid.Close();

            _openCount = _hid.Open(1, config.Vid, config.Pid, config.UsagePage, config.Usage);
            reconnecting = false;
            if (_openCount > 0)
            {
                new Thread(PollThread).Start();
            }
        }

        public static int[] bitPosMap =
        {
            23, 19, 22, 20, 21, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6
        };


        private unsafe void PollThread()
        {
            while (true)
            {
                if (_disposedValue) break;
                if (_hid == null) break;
                if (!IsConnected) continue;

                var len = 0;
                try
                {
                    len = _hid.Receive(0, ref _inBuffer, 64, 1000);
                }
                catch (Exception e)
                {
                    len = -1;
                }
                if (len < 0)
                {
                    break;
                }

                OutputData temp = new OutputData();
                temp.Buttons = new ArraySegment<byte>(_inBuffer, 0, 10).ToArray();
                temp.Lever = BitConverter.ToInt16(_inBuffer, 10);
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
            _openCount = 0;
        }

        public unsafe override void SetLed(byte[] data)
        {
            if (!IsConnected || _disposedValue || _hid == null)
                return;

            SetLedInput led;
            led.Type = 0;
            led.LedBrightness = 40;

            for (var i = 0; i < 9; i++)
            {
                led.LedColors[i] = data[i];
                led.LedColors[i + 15] = data[i + 9];
            }

            var outBuffer = new byte[64];
            fixed (void* d = outBuffer)
                Kernel32.CopyMemory(d, &led, 64);

            _hid.Send(0, 0, outBuffer, 64, 1000);
        }

        public override void Dispose()
        {
            _disposedValue = true;
            _openCount = 0;
            _hid?.Close();
            _hid = null;
        }
    }
    public class HidIOConfig
    {
        public uint Vid { get; set; } = 0x2341;
        public uint Pid { get; set; } = 0x8036;
        public int UsagePage { get; set; } = -1;
        public int Usage { get; set; } = -1;
    }

    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(HidIOConfig))]
    public partial class HidIOConfigContext : JsonSerializerContext
    {
    }
}