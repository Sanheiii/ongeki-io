using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using static MU3Input.KeyboardIO;

namespace MU3Input
{
    public class MixedIO : IO
    {
        private bool _disposedValue = false;
        private bool _isConnected = true;
        public override bool IsConnected => _isConnected;
        public override void Reconnect()
        {
            foreach (var item in Items)
            {
                item.Key.Reconnect();
            }
        }
        public Dictionary<IO, Scope> Items { get; }
        public override OutputData Data
        {
            get
            {
                var buttons = new byte[10];
                for (int i = 0; i < 10; i++)
                {
                    var ios = Items.Where(item => item.Value.HasFlag((Scope)(1 << i))).Select(io => io.Key);
                    foreach (var io in ios)
                    {
                        buttons[i] += io.Data.Buttons[i];
                    }
                }
                short lever = default;
                IO aimeIO = null;

                foreach (var item in Items)
                {
                    if (item.Value.HasFlag(Scope.Lever))
                        lever = item.Key.Data.Lever;
                    if (item.Value.HasFlag(Scope.Aime) && item.Key.Aime.Scan > 0)
                        aimeIO = item.Key;
                    if (!item.Key.IsConnected)
                        item.Key.Reconnect();
                }
                return new OutputData
                {
                    Buttons = buttons,
                    Lever = lever,
                    Aime = aimeIO?.Aime ?? new Aime(){Scan = 0,Data = new byte[18]},
                    OptButtons = Items.Select(item => item.Key.Data.OptButtons).Concat([OptButtons.None]).Aggregate((item1, item2) => item1 | item2),
                };
            }
        }

        public MixedIO()
        {
            Items = new Dictionary<IO, Scope>();
        }

        public IO CreateIO(IOType type, JsonValue param)
        {
            switch (type)
            {
                case IOType.Hid:
                    return new HidIO(JsonSerializer.Deserialize(param, HidIOConfigContext.Default.HidIOConfig));
                case IOType.Udp:
                    return new UdpIO(param.GetValue<int>());
                case IOType.Tcp:
                    return new TcpIO(param.GetValue<int>());
                case IOType.Usbmux:
                    return new UsbmuxIO(param.GetValue<ushort>());
                case IOType.Keyboard:
                    return new KeyboardIO(JsonSerializer.Deserialize(param, KeyboardIOConfigContext.Default.KeyboardIOConfig));
                default: throw new ArgumentException($"{type}: Unknown IO type");
            }
        }
        public void Add(IO io, Scope part)
        {
            Items.Add(io, part);
        }

        private uint currentLedData = 0;
        public override void SetLed(uint data)
        {
            currentLedData = data;
            foreach (IO io in Items.Keys) io.SetLed(currentLedData);
        }

        public override void Dispose()
        {
            _disposedValue = true;
            foreach (var io in Items.Keys)
            {
                io.Dispose();
            }

            _isConnected = false;
        }
    }
}
