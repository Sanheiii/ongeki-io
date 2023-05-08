using iMobileDevice;
using iMobileDevice.iDevice;
using iMobileDevice.Lockdown;

using SimpleHID.Raw;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Timers;

namespace MU3Input
{
    public class UsbmuxIO : IO
    {
        private ushort remotePort = 4354;
        iDeviceConnectionHandle connection;
        private IiDeviceApi iDevice = LibiMobileDevice.Instance.iDevice;
        private ILockdownApi lockdown = LibiMobileDevice.Instance.Lockdown;
        iDeviceEventCallBack deviceEventCallBack;
        public List<string> Devices = new List<string>();
        protected OutputData data;
        public override bool IsConnected => !(connection?.IsClosed ?? true);
        public override OutputData Data => data;
        static UsbmuxIO()
        {
            NativeLibraries.Load();
        }
        ~UsbmuxIO()
        {
            connection?.Close();
        }
        public UsbmuxIO(ushort remotePort)
        {
            this.remotePort = remotePort;
            deviceEventCallBack = new iDeviceEventCallBack(DeviceEventCallback);
            data = new OutputData() { Buttons = new byte[10], Aime = new Aime() { Data = new byte[18] } };
            iDevice.idevice_event_subscribe(deviceEventCallBack, IntPtr.Zero);
            new Thread(PollThread).Start();
        }

        bool connecting = false;
        public override void Reconnect()
        {
            Disconnect();
            Connect();
        }
        public void Disconnect()
        {
            connection?.Close();
        }
        public void Connect()
        {
            if (connecting) return;
            if (IsConnected) return;
            connecting = true;
            string[] devices = Devices.ToArray();
            foreach (var device in devices)
            {
                if (!ConnectByUdid(device, out connection).IsError())
                {
                    SetLed(currentLedData);
                    break;
                }
            }
            connecting = false;
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
                uint len = 0;
                iDeviceError error = iDevice.idevice_connection_receive(connection, _inBuffer, 1, ref len);
                if (error.IsError())
                {
                    Reconnect();
                    continue;
                }
                Receive((MessageType)_inBuffer[0]);
            }
        }
        private unsafe void Receive(MessageType type)
        {
            uint len = 0;
            if (type == MessageType.ButtonStatus && !iDevice.idevice_connection_receive(connection, _inBuffer, 2, ref len).IsError())
            {
                int index = _inBuffer[0];
                data.Buttons[index] = _inBuffer[1];
            }
            else if (type == MessageType.MoveLever && !iDevice.idevice_connection_receive(connection, _inBuffer, 2, ref len).IsError())
            {
                var value = (short)(_inBuffer[1] << 8 | _inBuffer[0]);
                data.Lever = value;
            }
            else if (type == MessageType.Scan && !iDevice.idevice_connection_receive(connection, _inBuffer, 1, ref len).IsError())
            {
                data.Aime.Scan = _inBuffer[0];
                if (data.Aime.Scan == 0)
                {

                }
                else if (data.Aime.Scan == 1 && !iDevice.idevice_connection_receive(connection, _inBuffer, 10, ref len).IsError())
                {
                    byte[] aimeId = new ArraySegment<byte>(_inBuffer, 0, 10).ToArray();
                    if (aimeId.All(n => n == 255))
                    {
                        aimeId = Utils.ReadOrCreateAimeTxt();
                    }
                    data.Aime.ID = aimeId;
                }
                else if (data.Aime.Scan == 2 && !iDevice.idevice_connection_receive(connection, _inBuffer, 18, ref len).IsError())
                {
                    data.Aime.IDm = BitConverter.ToUInt64(_inBuffer, 0);
                    data.Aime.PMm = BitConverter.ToUInt64(_inBuffer, 8);
                    data.Aime.SystemCode = BitConverter.ToUInt16(_inBuffer, 16);
                }
            }
            else if (type == MessageType.Test && !iDevice.idevice_connection_receive(connection, _inBuffer, 1, ref len).IsError())
            {
                if (_inBuffer[1] == 0) data.OptButtons &= ~OptButtons.Test;
                else data.OptButtons |= OptButtons.Test;
                Debug.WriteLine(Data.OptButtons);
            }
            else if (type == MessageType.Service && !iDevice.idevice_connection_receive(connection, _inBuffer, 1, ref len).IsError())
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
            else if (type == MessageType.Hello && !iDevice.idevice_connection_receive(connection, _inBuffer, 1, ref len).IsError())
            {
                uint sendBytes = 0;
                iDevice.idevice_connection_send(connection, new byte[] { (byte)MessageType.Hello, _inBuffer[0] }, 2, ref sendBytes);
            }
        }

        private void SetLever(short lever)
        {
            try
            {
                if (!IsConnected)
                    return;
                uint sendBytes = 0;
                iDevice.idevice_connection_send(connection, new byte[] { (byte)MessageType.SetLever }.Concat(BitConverter.GetBytes(lever)).ToArray(), 3, ref sendBytes);
            }
            catch
            {
                return;
            }
        }

        private uint currentLedData = 0;
        public override void SetLed(uint data)
        {
            try
            {
                // 缓存led数据将其设置到新连接的设备
                currentLedData = data;
                if (!IsConnected)
                    return;
                uint sendBytes = 0;
                iDevice.idevice_connection_send(connection, new byte[] { (byte)MessageType.SetLed }.Concat(BitConverter.GetBytes(data)).ToArray(), 5, ref sendBytes);
            }
            catch
            {
                return;
            }
        }
        private iDeviceError ConnectByUdid(string udid, out iDeviceConnectionHandle connection)
        {
            iDevice.idevice_new(out iDeviceHandle deviceHandle, udid);
            return iDevice.idevice_connect(deviceHandle, remotePort, out connection);
        }
        private void DeviceEventCallback(ref iDeviceEvent e, IntPtr userData)
        {
            var udid = e.udidString;
            switch (e.@event)
            {
                case iDeviceEventType.DeviceAdd:
                    if (!Devices.Any(d => d.Equals(udid)))
                    {
                        Devices.Add(udid);
                    }
                    break;
                case iDeviceEventType.DeviceRemove:
                    var value = Devices.First(d => d.Equals(udid));
                    Devices.Remove(value);
                    break;
                case iDeviceEventType.DevicePaired:
                    break;
                default:
                    break;
            }
        }
        private string GetDeviceName(string udid)
        {
            iDevice.idevice_new(out iDeviceHandle deviceHandle, udid).ThrowOnError();
            LockdownClientHandle lockdownHandle;
            lockdown.lockdownd_client_new_with_handshake(deviceHandle, out lockdownHandle, "geki").ThrowOnError();
            string deviceName;
            lockdown.lockdownd_get_device_name(lockdownHandle, out deviceName).ThrowOnError();
            deviceHandle.Dispose();
            lockdownHandle.Dispose();
            return deviceName;
        }
    }
}
