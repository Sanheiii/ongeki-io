using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace MU3Input
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 64)]
    public struct OutputData
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10, ArraySubType = UnmanagedType.U1)]
        public byte[] Buttons;

        public short Lever;

        public OptButtons OptButtons;

        public Aime Aime;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 64)]
    public unsafe struct SetLedInput
    {
        public byte Type;
        public byte LedBrightness;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public fixed byte LedColors[3 * 10];
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 64)]
    public unsafe struct SetOptionInput
    {
        public byte Type;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public fixed byte AimiId[10];
    }
    public abstract class IO
    {
        public abstract OutputData Data { get; }

        private byte[] leftButtonsCache = new byte[5];
        private byte[] rightButtonsCache = new byte[5];
        public byte LeftButton
        {
            get
            {
                byte result = 0;
                for (int i = 4; i >= 0; i--)
                {
                    result <<= 1;
                    // 按钮触点数量不为0时
                    if (Data.Buttons[i] > 0)
                    {
                        // 当已被按下并增加触点数量时自动松开一帧
                        if (leftButtonsCache[i] > 0 && Data.Buttons[i] > leftButtonsCache[i])
                        {
                            result += 0;
                        }
                        else
                        {
                            result += 1;
                        }
                    }
                }
                Array.Copy(Data.Buttons, 0, leftButtonsCache, 0, 5);
                return result;
            }
        }

        public byte RightButton
        {
            get
            {
                byte result = 0;
                for (int i = 4; i >= 0; i--)
                {
                    result <<= 1;
                    if (Data.Buttons[i + 5] > 0)
                    {
                        if (rightButtonsCache[i] > 0 && Data.Buttons[i + 5] > rightButtonsCache[i])
                        {
                            result += 0;
                        }
                        else
                        {
                            result += 1;
                        }
                    }
                }
                Array.Copy(Data.Buttons, 5, rightButtonsCache, 0, 5);
                return result;
            }
        }

        public short Lever
        {
            get
            {
                return Data.Lever;
            }
        }
        public Aime Aime => Data.Aime;
        public OptButtons OptButtonsStatus => Data.OptButtons;

        public abstract bool IsConnected { get; }
        public abstract void Reconnect();
        public abstract void SetLed(uint data);
    }
    [Flags]
    public enum OptButtons : byte
    {
        None = 0b000,
        Test = 0b001,
        Service = 0b010,
        Coin = 0b100
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 19)]
    public struct Aime
    {
        [MarshalAs(UnmanagedType.U1)]
        public byte Scan;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18, ArraySubType = UnmanagedType.U1)]
        public byte[] Data;

        public byte[] ID
        {
            get => new ArraySegment<byte>(Data, 0, 10).ToArray();
            set => value.CopyTo(Data, 0);
        }

        public ulong IDm
        {
            get => BitConverter.ToUInt64(Data, 0);
            set => BitConverter.GetBytes(value).CopyTo(Data, 0);
        }
        public ulong PMm
        {
            get => BitConverter.ToUInt64(Data, 8);
            set => BitConverter.GetBytes(value).CopyTo(Data, 8);
        }
        public ushort SystemCode
        {
            get => BitConverter.ToUInt16(Data, 16);
            set => BitConverter.GetBytes(value).CopyTo(Data, 16);
        }
    }
    enum MessageType : byte
    {
        // 控制器向IO发送的
        ButtonStatus = 1,
        MoveLever = 2,
        Scan = 3,
        Test = 4,
        Service = 5,
        RequestValues = 6,
        // IO向控制器发送的
        SetLed = 20,
        SetLever = 21,
        // 寻找在线设备
        Hello = 255
    }
}
