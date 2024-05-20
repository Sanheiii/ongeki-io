using CommunityToolkit.Mvvm.ComponentModel;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Timers;

namespace IOConfig
{
    public class Config
    {
        public static Config Instance;
        private static string configPath;
        private static Timer saveTimer;

        public ObservableCollection<ConfigItem> IO { get; private set; } = new();

        static Config()
        {
            Instance = new Config();
            var directoryName = Directory.GetCurrentDirectory();
            configPath = Path.Combine(directoryName, "mu3input_config.json");
            if (File.Exists(configPath))
            {
                JsonSerializerOptions options = new JsonSerializerOptions()
                {
                    Converters =
                {
                    new ConfigConverter(),
                    new JsonStringEnumConverter<IOType>(),
                    new JsonStringEnumConverter<Scope>()
                }
                };
                Instance = JsonSerializer.Deserialize<Config>(File.ReadAllText(configPath), options);
            }
            else
            {
                Instance = new Config();
            }

            Instance.IO.CollectionChanged += Instance.OnCollectionChanged;
            foreach (var io in Instance.IO)
            {
                io.PropertyChanged += Instance.OnIOPropertyChanged;
                if (io.Param is INotifyPropertyChanged observable) observable.PropertyChanged += Instance.OnIOPropertyChanged;
            }

            saveTimer = new Timer(1000);
            saveTimer.Elapsed += (sender, e) => Instance.Save();
            saveTimer.AutoReset = false;
        }

        public void Save()
        {
            if (Mu3IO.IsBusy())
            {
                ResetSaveTimer();
                return;
            }
            Save(configPath);
        }

        public void Save(string path)
        {
            File.WriteAllText(path, JsonSerializer.Serialize(this, new JsonSerializerOptions{WriteIndented = true}));
            Mu3IO.Init();
            AimeIO.Init();
        }

        private void ResetSaveTimer()
        {
            saveTimer.Stop();
            saveTimer.Start();
        }

        private void OnCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (ConfigItem newItem in e.NewItems)
                {
                    newItem.PropertyChanged += OnIOPropertyChanged;
                    if (newItem.Param is INotifyPropertyChanged observable) observable.PropertyChanged += Instance.OnIOPropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (ConfigItem oldItem in e.OldItems)
                {
                    oldItem.PropertyChanged -= OnIOPropertyChanged;
                    if (oldItem.Param is INotifyPropertyChanged observable) observable.PropertyChanged -= Instance.OnIOPropertyChanged;
                }
            }

            ResetSaveTimer();
        }

        private void OnIOPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var property = sender.GetType().GetProperty(e.PropertyName);
            if (property?.GetCustomAttribute(typeof(JsonIgnoreAttribute)) != null) return;
            ResetSaveTimer();
        }
    }

    public class ConfigItem : ObservableObject
    {
        [JsonConverter(typeof(JsonStringEnumConverter<IOType>))]
        public IOType Type
        {
            get => type;
            set => SetProperty(ref type, value);
        }
        private IOType type;

        public object Param
        {
            get => param;
            set => SetProperty(ref param, value);
        }
        private object param;

        [JsonConverter(typeof(JsonStringEnumConverter<Scope>))]
        public Scope Scope
        {
            get => scope;
            set => SetProperty(ref scope, value);
        }
        private Scope scope;

        [JsonIgnore]
        public bool IsConnected
        {
            get => isConnected;
            set => SetProperty(ref isConnected, value);
        }
        private bool isConnected;
    }


    public enum IOType
    {
        Hid, Udp, Tcp, Usbmux, Keyboard
    }

    public partial class KeyboardParam : ObservableObject
    {
        [ObservableProperty]
        private int l1 = -1;
        [ObservableProperty]
        private int l2 = -1;
        [ObservableProperty]
        private int l3  = -1;
        [ObservableProperty]
        private int lSide  = -1;
        [ObservableProperty]
        private int lMenu  = -1;
        [ObservableProperty]
        private int r1  = -1;
        [ObservableProperty]
        private int r2  = -1;
        [ObservableProperty]
        private int r3  = -1;
        [ObservableProperty]
        private int rSide  = -1;
        [ObservableProperty]
        private int rMenu  = -1;
        [ObservableProperty]
        private int test  = -1;
        [ObservableProperty]
        private int service  = -1;
        [ObservableProperty]
        private int coin  = -1;
        [ObservableProperty]
        private int scan  = -1;
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

    public class ConfigConverter : JsonConverter<Config>
    {
        public override Config Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using JsonDocument doc = JsonDocument.ParseValue(ref reader);
            JsonElement root = doc.RootElement;
            Config result = new Config();
            foreach (var jio in root.GetProperty("IO").EnumerateArray())
            {
                ConfigItem configItem = new ConfigItem();
                configItem.Type = jio.GetProperty("Type").Deserialize<IOType>(options);
                configItem.Param = configItem.Type switch
                {
                    IOType.Keyboard => jio.GetProperty("Param").Deserialize<KeyboardParam>(),
                    IOType.Tcp => jio.GetProperty("Param").Deserialize<ushort>(),
                    IOType.Udp => jio.GetProperty("Param").Deserialize<ushort>(),
                    IOType.Usbmux => jio.GetProperty("Param").Deserialize<ushort>(),
                    _ => jio.GetProperty("Param").Deserialize<object>()
                };
                configItem.Scope = jio.GetProperty("Scope").Deserialize<Scope>(options);

                result.IO.Add(configItem);
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, Config value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}