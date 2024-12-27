using System.Text;
using System.Text.Json.Serialization;

namespace MU3Input
{
    internal class KeyboardIO : IO
    {
        private KeyboardIOConfig config;
        public override OutputData Data => GetData();
        private bool _isConnected = true;
        public override bool IsConnected => _isConnected;

        public KeyboardIO(KeyboardIOConfig param)
        {
            config = param;
        }

        public override void Reconnect() { }

        public override void SetLed(uint data) { }

        public override void Dispose()
        {
            _isConnected = false;
        }


        StringBuilder sb = new StringBuilder();
        private OutputData GetData()
        {
            //if (!Utils.IsForeground())
            //{
            //    return new OutputData() { Buttons = new byte[10], Aime = new Aime() { Data = new byte[18] } };
            //}

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
            byte testPressed = Pressed(config.Test);
            byte servicePressed = Pressed(config.Service);
            byte coinPressed = Pressed(config.Coin);
            OptButtons optButtons = (OptButtons)(testPressed << 0 | servicePressed << 1 | coinPressed << 2);
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
        private byte Pressed(int key)
        {
            return User32.GetAsyncKeyState(key) == 0 ? (byte)0 : (byte)1;
        }
    }
    public class KeyboardIOConfig
    {
        public int L1 { get; set; } = -1;
        public int L2 { get; set; } = -1;
        public int L3 { get; set; } = -1;
        public int LSide { get; set; } = -1;
        public int LMenu { get; set; } = -1;
        public int R1 { get; set; } = -1;
        public int R2 { get; set; } = -1;
        public int R3 { get; set; } = -1;
        public int RSide { get; set; } = -1;
        public int RMenu { get; set; } = -1;
        public int Test { get; set; } = -1;
        public int Service { get; set; } = -1;
        public int Coin { get; set; } = -1;
        public int Scan { get; set; } = -1;
    }

    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(KeyboardIOConfig))]
    public partial class KeyboardIOConfigContext : JsonSerializerContext
    {
    }
}
