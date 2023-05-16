using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;

using static MU3Input.KeyboardIO;

namespace MU3Input
{
    public class MixedIO : IO
    {
        public override bool IsConnected => true;
        public override void Reconnect()
        {
            foreach (var item in Items)
            {
                item.Key.Reconnect();
            }
        }
        public Dictionary<IO, ControllerPart> Items { get; }
        public override OutputData Data
        {
            get
            {
                var buttons = new byte[10];
                for (int i = 0; i < 10; i++)
                {
                    var io = Items.FirstOrDefault(item => item.Value.HasFlag((ControllerPart)(1 << i))).Key;
                    buttons[i] = io == null ? (byte)0 : io.Data.Buttons[i];
                }
                short lever = default;
                IO aimeIO = null;

                foreach (var item in Items)
                {
                    if (item.Value.HasFlag(ControllerPart.Lever))
                        lever = item.Key.Data.Lever;
                    if (item.Value.HasFlag(ControllerPart.Aime))
                        aimeIO = item.Key;
                    if (!item.Key.IsConnected)
                        item.Key.Reconnect();
                }
                return new OutputData
                {
                    Buttons = buttons,
                    Lever = lever,
                    Aime = aimeIO?.Aime ?? default,
                    OptButtons = Items.Select(item => item.Key.Data.OptButtons).Aggregate((item1, item2) => item1 | item2),
                };
            }
        }

        public MixedIO()
        {
            Items = new Dictionary<IO, ControllerPart>();
        }

        public IO CreateIO(IOType type, JToken param)
        {
            switch (type)
            {
                case IOType.Hid:
                    return new HidIO();
                //case IOType.Udp:
                //    return new UdpIO(param.Value<int>());
                //case IOType.Tcp:
                //    return new TcpIO(param.Value<int>());
                //case IOType.Usbmux:
                //    return new UsbmuxIO(param.Value<ushort>());
                case IOType.Keyboard:
                    return new KeyboardIO(param.ToObject<KeyboardIOConfig>());
                default: throw new ArgumentException($"{type}: Unknown IO type");
            }
        }
        public void Add(IO io, ControllerPart part)
        {
            if (Check(part, Items.Select(i => i.Value).ToArray()))
            {
                Items.Add(io, part);
            }
        }
        public void Remove(IO io)
        {
            Items.Remove(io);
        }

        public void Modify(IO io, ControllerPart part)
        {
            var parts = Items.Where(item => item.Key != io).Select(item => item.Value).ToArray();
            if (Check(part, parts))
            {
                Items[io] = part;
            }
        }

        public bool Check(ControllerPart part1, params ControllerPart[] parts)
        {
            return true;
            if (parts.Length == 0) return true;
            ControllerPart part2 = parts.Aggregate((p1, p2) => p1 | p2);
            return (part1 & part2) == ControllerPart.None;
        }

        private uint currentLedData = 0;
        public override void SetLed(uint data)
        {
            currentLedData = data;
            foreach (IO io in Items.Keys) io.SetLed(currentLedData);
        }
    }
}
