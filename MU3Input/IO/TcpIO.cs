using SimpleHID.Raw;

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace MU3Input
{
    public class TcpIO : IO
    {
        private string ip = "127.0.0.1";
        private int port;
        private uint currentLedData = 0;
        private bool connecting = false;
        private TcpClient client;
        private NetworkStream networkStream;
        protected OutputData data;

        public TcpIO(int port)
        {
            this.port = port;
            data = new OutputData() { Buttons = new byte[10], Aime = new Aime() { Data = new byte[18] } };
            new Thread(PollThread).Start();

        }
        public override bool IsConnected => client?.Connected ?? false;
        public override OutputData Data => data;
        // 重连
        public override void Reconnect()
        {
            if (connecting) return;
            Disconnect();
            ConnectAsync(ip, port);
            //connectTask?.Wait();
        }
        public void ConnectAsync(string ip, int port)
        {
            if (connecting) return;
            connecting = true;
            try
            {
                var newClient = new TcpClient(ip, port);
                networkStream = newClient.GetStream();
                client = newClient;
                SetLed(currentLedData);
            }
            catch (Exception)
            {
                Disconnect();
            }
            connecting = false;
        }
        private void Disconnect()
        {
            if (IsConnected)
            {
                var tmpClient = client;
                var tmpStream = networkStream;
                client = null;
                networkStream = null;
                tmpClient?.Dispose();
                tmpStream?.Dispose();
            }
        }
        private byte[] _inBuffer = new byte[32];
        private unsafe void PollThread()
        {
            while (true)
            {
                if (!IsConnected)
                {
                    Reconnect();
                    continue;
                }
                int len = networkStream.Read(_inBuffer, 0, 1);
                if (len <= 0)
                {
                    Reconnect();
                    continue;
                }
                Receive((MessageType)_inBuffer[0]);
            }
        }
        private unsafe void Receive(MessageType type)
        {
            if (type == MessageType.ButtonStatus && networkStream.Read(_inBuffer, 0, 2) > 0)
            {
                int index = _inBuffer[0];
                data.Buttons[index] = _inBuffer[1];
            }
            else if (type == MessageType.MoveLever && networkStream.Read(_inBuffer, 0, 2) > 0)
            {
                var value = (short)(_inBuffer[1] << 8 | _inBuffer[0]);
                data.Lever = value;
            }
            else if (type == MessageType.Scan && networkStream.Read(_inBuffer, 0, 1) > 0)
            {
                data.Aime.Scan = _inBuffer[0];
                if (data.Aime.Scan == 0)
                {

                }
                else if (data.Aime.Scan == 1 && networkStream.Read(_inBuffer, 0, 10) > 0)
                {
                    byte[] aimeId = new ArraySegment<byte>(_inBuffer, 0, 10).ToArray();
                    if (aimeId.All(n => n == 255))
                    {
                        aimeId = Utils.ReadOrCreateAimeTxt();
                    }
                    data.Aime.ID = aimeId;
                }
                else if (data.Aime.Scan == 2 && networkStream.Read(_inBuffer, 0, 18) > 0)
                {
                    data.Aime.IDm = BitConverter.ToUInt64(_inBuffer, 0);
                    data.Aime.PMm = BitConverter.ToUInt64(_inBuffer, 8);
                    data.Aime.SystemCode = BitConverter.ToUInt16(_inBuffer, 16);
                }
            }
            else if (type == MessageType.Test && networkStream.Read(_inBuffer, 0, 1) > 0)
            {
                if (_inBuffer[1] == 0) data.OptButtons &= ~OptButtons.Test;
                else data.OptButtons |= OptButtons.Test;
                Debug.WriteLine(Data.OptButtons);
            }
            else if (type == MessageType.Service && networkStream.Read(_inBuffer, 0, 1) > 0)
            {
                if (_inBuffer[1] == 0) data.OptButtons &= ~OptButtons.Service;
                else data.OptButtons |= OptButtons.Service;
                Debug.WriteLine(Data.OptButtons);
            }
            else if (type == MessageType.RequestValues)
            {
                SetLed(currentLedData);
                SetLever(Data.Lever);
            }
            // 收到心跳数据直接回传原数据表示在线
            else if (type == MessageType.Hello && networkStream.Read(_inBuffer, 0, 1) > 0)
            {
                networkStream.Write(new byte[] { (byte)MessageType.Hello, _inBuffer[0] }, 0, 2);
            }
        }

        private void SetLever(short lever)
        {
            try
            {
                if (!IsConnected)
                    return;
                networkStream.Write(new byte[] { (byte)MessageType.SetLever }.Concat(BitConverter.GetBytes(lever)).ToArray(), 0, 3);
            }
            catch
            {
                return;
            }
        }

        public override unsafe void SetLed(uint data)
        {
            try
            {
                // 缓存led数据将其设置到新连接的设备
                currentLedData = data;
                if (!IsConnected)
                    return;
                networkStream.Write(new byte[] { (byte)MessageType.SetLed }.Concat(BitConverter.GetBytes(data)).ToArray(), 0, 5);
            }
            catch
            {
                return;
            }
        }
    }
}
