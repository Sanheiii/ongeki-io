using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;

namespace MU3Input
{
    public class UdpIO : IO
    {
        uint currentLedData = 0;
        UdpClient client;
        IPEndPoint savedEP;
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
        System.Timers.Timer timer = new System.Timers.Timer(1500)
        {
            AutoReset = false
        };
        protected OutputData data;

        public UdpIO(int port)
        {
            data = new OutputData() { Buttons = new byte[10], Aime = new Aime() { Data = new byte[18] } };
            client = new UdpClient(port);
            timer.Elapsed += Timer_Elapsed;
            new Thread(PollThread).Start();
        }

        // 一段时间没收到心跳包自动断开以接受新的连接
        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            isConnected = false;
            savedEP = null;
        }

        bool isConnected = false;
        public override bool IsConnected => isConnected;
        public override OutputData Data => data;
        public override void Reconnect() { }

        private void PollThread()
        {
            while (true)
            {
                byte[] buffer = client?.Receive(ref remoteEP);
                // 如果已连接设备但收到了其他设备的消息则忽略
                if (IsConnected && (!remoteEP.Address.Equals(savedEP?.Address))) return;
                ParseBuffer(buffer);
            }
        }

        private unsafe void ParseBuffer(byte[] buffer)
        {
            if ((buffer?.Length ?? 0) == 0) return;
            if (buffer[0] == (byte)MessageType.ButtonStatus && buffer.Length == 3)
            {
                int index = buffer[1];
                data.Buttons[index] = buffer[2];
            }
            else if (buffer[0] == (byte)MessageType.MoveLever && buffer.Length == 3)
            {
                var value = (short)(buffer[2] << 8 | buffer[1]);
                data.Lever = value;
            }
            else if (buffer[0] == (byte)MessageType.Scan && (buffer.Length == 2 || buffer.Length == 12 || buffer.Length == 20))
            {
                data.Aime.Scan = buffer[1];
                if (data.Aime.Scan == 0)
                {

                }
                else if (data.Aime.Scan == 1)
                {
                    byte[] aimeId = new ArraySegment<byte>(buffer, 2, 10).ToArray();
                    if (aimeId.All(n => n == 255))
                    {
                        aimeId = Utils.ReadOrCreateAimeTxt();
                    }
                    data.Aime.ID = aimeId;
                }
                else if (data.Aime.Scan == 2)
                {
                    data.Aime.IDm = BitConverter.ToUInt64(buffer, 2);
                    data.Aime.PMm = BitConverter.ToUInt64(buffer, 10);
                    data.Aime.SystemCode = BitConverter.ToUInt16(buffer, 18);
                }
            }
            else if (buffer[0] == (byte)MessageType.Test && buffer.Length == 2)
            {
                if (buffer[1] == 0) data.OptButtons &= ~OptButtons.Test;
                else data.OptButtons |= OptButtons.Test;
                Debug.WriteLine(Data.OptButtons);
            }
            else if (buffer[0] == (byte)MessageType.Service && buffer.Length == 2)
            {
                if (buffer[1] == 0) data.OptButtons &= ~OptButtons.Service;
                else data.OptButtons |= OptButtons.Service;
                Debug.WriteLine(Data.OptButtons);
            }
            else if (buffer[0] == (byte)MessageType.RequestValues && buffer.Length == 1)
            {
                SetLed(currentLedData);
                SetLever(Data.Lever);
            }
            // 收到心跳数据直接回传原数据表示在线，并保存其地址阻止其他设备连接
            else if (buffer[0] == (byte)MessageType.Hello && buffer.Length == 2)
            {
                savedEP = new IPEndPoint(remoteEP.Address, remoteEP.Port);
                client.SendAsync(buffer, 2, savedEP);
                isConnected = true;
                timer.Stop();
                timer.Start();
            }
        }
        private void SetLever(short lever)
        {
            if (savedEP != null)
            {
                client?.SendAsync(new byte[] { (byte)MessageType.SetLever }.Concat(BitConverter.GetBytes(lever)).ToArray(), 3, savedEP);
            }
        }

        public override unsafe void SetLed(uint data)
        {
            currentLedData = data;
            if (savedEP != null)
            {
                client?.SendAsync(new byte[] { (byte)MessageType.SetLed }.Concat(BitConverter.GetBytes(data)).ToArray(), 5, savedEP);
            }
        }

    }
}
