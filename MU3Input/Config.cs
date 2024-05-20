using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MU3Input
{
    public class Config
    {
        private static string configPath;


        public static Config? Load()
        {
            Config res;
            var directoryName = Directory.GetCurrentDirectory();
            configPath = Path.Combine(directoryName, "mu3input_config.json");
            if (File.Exists(configPath))
            {
                res = JsonSerializer.Deserialize(File.ReadAllText(configPath), ConfigContext.Default.Config);
            }
            else
            {
                res = GetDefault();
                res.Save(configPath);
            }

            return res;
        }

        private static Config GetDefault()
        {
            var config = new Config
            {
                IO =
                [
                    new IOConfig
                    {
                        Type = IOType.Udp,
                        Param = JsonValue.Create(4354),
                        Scope = Scope.All
                    }
                ]
            };
            return config;
        }

        public void Save()
        {
            Save(configPath);
        }

        public void Save(string path)
        {
            File.WriteAllText(path, JsonSerializer.Serialize(this, ConfigContext.Default.Config));
        }

        public List<IOConfig> IO { get; set; } = new();
    }
    public class IOConfig
    {
        [JsonConverter(typeof(JsonStringEnumConverter<IOType>))]
        public IOType Type { get; set; }
        public JsonValue Param { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter<Scope>))]
        public Scope Scope { get; set; }
    }


    public enum IOType
    {
        Hid, Udp, Tcp, Usbmux, Keyboard
    }

    [Flags]
    public enum Scope
    {
        None = 0,
        L1 = 1 << 0,
        L2 = 1 << 1,
        L3 = 1 << 2,
        LSide = 1 << 3,
        LMenu = 1 << 4,
        R1 = 1 << 5,
        R2 = 1 << 6,
        R3 = 1 << 7,
        RSide = 1 << 8,
        RMenu = 1 << 9,
        Lever = 1 << 10,
        Aime = 1 << 11,
        LKeyBoard = L1 | L2 | L3,
        RKeyBoard = R1 | R2 | R3,
        Side = LSide | RSide,
        Menu = LMenu | RMenu,
        KeyBoard = LKeyBoard | RKeyBoard,
        Left = LKeyBoard | LSide | LMenu,
        Right = RKeyBoard | RSide | RMenu,
        GameButtons = KeyBoard | Side,
        Buttons = GameButtons | Menu,
        GamePlay = GameButtons | Lever,
        All = GamePlay | Menu | Aime,
    }

    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(Config))]
    public partial class ConfigContext : JsonSerializerContext
    {
    }
}
