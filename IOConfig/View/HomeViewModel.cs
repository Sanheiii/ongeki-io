using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace IOConfig
{
    public partial class HomeViewModel : ObservableObject
    {
        public ObservableCollection<ConfigItem> IOConfigs => Config.Instance.IO;

        [ObservableProperty]
        private byte left = 0;
        [ObservableProperty]
        private byte right = 0;
        [ObservableProperty]
        private byte opButton = 0;
        [ObservableProperty]
        private short lever = 0;

        [ObservableProperty]
        private string aime;



        public HomeViewModel()
        {
            Mu3IO.Init();
            AimeIO.Init();
        }

        [RelayCommand]
        private void RemoveController(ConfigItem configItem)
        {
            IOConfigs.Remove(configItem);
        }

        public unsafe void UpdateStates()
        {
            byte left;
            byte right;
            byte opButton;
            short lever;
            Mu3IO.Poll();
            Mu3IO.GetGameButtons(&left, &right);
            Left = left;
            Right = right;
            Mu3IO.GetOpButtons(&opButton);
            OpButton = opButton;
            Mu3IO.GetLever(&lever);
            Lever = lever; 
            for (int i = 0; i < IOConfigs.Count; i++)
            {
                IOConfigs[i].IsConnected = Mu3IO.IsConnected(i);
            }


            byte[] luid = new byte[10];
            ulong id = 0;
            fixed (byte* data = luid)
            {
                if (AimeIO.GetAimeId(0, data, 10) == 0)
                {
                    Aime = Bcd2Str(luid);
                }
                else if (AimeIO.GetFelicaId(0, &id) == 0)
                {
                    Aime = $"0x{id:X16}";
                }
                else
                {
                    Aime = "无";
                }
            }

        }
        internal static string Bcd2Str(byte[] bytes)
        {
            StringBuilder stringBuilder = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
            {
                stringBuilder.Append(bytes[i].ToString("x2"));
            }
            return stringBuilder.ToString();
        }

    }
}
