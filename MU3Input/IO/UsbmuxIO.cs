using Netimobiledevice;
using Netimobiledevice.Usbmuxd;

using System.Diagnostics;
using System.Net.Sockets;

namespace MU3Input;

public class UsbmuxIO : IO
{
    private bool _disposedValue = false;
    private ushort remotePort = 4354;
    Socket connection;
    public List<string> Devices = new List<string>();
    protected OutputData data;
    public override bool IsConnected => connection?.Connected ?? false;
    public override OutputData Data => data;

    ~UsbmuxIO()
    {
        connection?.Close();
    }
    public UsbmuxIO(ushort remotePort)
    {
        this.remotePort = remotePort;
        data = new OutputData() { Buttons = new byte[10], Aime = new Aime() { Data = new byte[18] } };
        Usbmux.Subscribe(DeviceEventCallback);
        new Thread(PollThread).Start();
    }

    bool connecting = false;
    public override void Reconnect()
    {
        Disconnect();
        //Connect();
    }
    public void Disconnect()
    {
        connection?.Close();
    }
    public void Connect()
    {
        if (connecting || IsConnected || _disposedValue) return;
        connecting = true;
        string[] devices = Devices.ToArray();
        foreach (var device in devices)
        {
            connection = ConnectByUdid(device);
            if (connection?.Connected ?? false)
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
            if(_disposedValue) return;
            if (!IsConnected)
            {
                Task.Run(Connect);
                Thread.Sleep(10);
                continue;
            }

            int len = 0;
            if (!Receive(connection, _inBuffer, 1, ref len))
            {
                Disconnect();
                continue;
            }
            Receive((MessageType)_inBuffer[0]);
        }
    }
    private unsafe void Receive(MessageType type)
    {
        int len = 0;
        if (type == MessageType.ButtonStatus && Receive(connection, _inBuffer, 2, ref len))
        {
            int index = _inBuffer[0];
            data.Buttons[index] = _inBuffer[1];
        }
        else if (type == MessageType.MoveLever && Receive(connection, _inBuffer, 2, ref len))
        {
            var value = (short)(_inBuffer[1] << 8 | _inBuffer[0]);
            data.Lever = value;
        }
        else if (type == MessageType.Scan && Receive(connection, _inBuffer, 1, ref len))
        {
            data.Aime.Scan = _inBuffer[0];
            if (data.Aime.Scan == 0)
            {

            }
            else if (data.Aime.Scan == 1 && Receive(connection, _inBuffer, 10, ref len))
            {
                byte[] aimeId = new ArraySegment<byte>(_inBuffer, 0, 10).ToArray();
                if (aimeId.All(n => n == 255))
                {
                    aimeId = Utils.ReadOrCreateAimeTxt();
                }
                data.Aime.ID = aimeId;
            }
            else if (data.Aime.Scan == 2 && Receive(connection, _inBuffer, 18, ref len))
            {
                data.Aime.IDm = BitConverter.ToUInt64(_inBuffer, 0);
                data.Aime.PMm = BitConverter.ToUInt64(_inBuffer, 8);
                data.Aime.SystemCode = BitConverter.ToUInt16(_inBuffer, 16);
            }
        }
        else if (type == MessageType.Test && Receive(connection, _inBuffer, 1, ref len))
        {
            if (_inBuffer[1] == 0) data.OptButtons &= ~OptButtons.Test;
            else data.OptButtons |= OptButtons.Test;
            Debug.WriteLine(Data.OptButtons);
        }
        else if (type == MessageType.Service && Receive(connection, _inBuffer, 1, ref len))
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
        else if (type == MessageType.Hello && Receive(connection, _inBuffer, 1, ref len))
        {
            int sendBytes = 0;
            Send(connection, new byte[] { (byte)MessageType.Hello, _inBuffer[0] }, 2, ref sendBytes);
        }
    }

    private void SetLever(short lever)
    {
        try
        {
            if (!IsConnected)
                return;
            int sendBytes = 0;
            Send(connection, new byte[] { (byte)MessageType.SetLever }.Concat(BitConverter.GetBytes(lever)).ToArray(), 3, ref sendBytes);
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
            int sendBytes = 0;
            Send(connection, new byte[] { (byte)MessageType.SetLed }.Concat(BitConverter.GetBytes(data)).ToArray(), 5, ref sendBytes);
        }
        catch
        {
            return;
        }
    }

    public override void Dispose()
    {
        _disposedValue = true;
        Disconnect();
    }

    private Socket? ConnectByUdid(string udid)
    {
        try
        {
            var device = Usbmux.GetDevice(udid);
            return device?.Connect(remotePort);
        }
        catch { return null; }
    }

    private bool Receive(Socket connection, byte[] buffer, int size, ref int len)
    {
        try
        {
            len = connection.Receive(buffer, size, SocketFlags.None);
            return len == size;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }

    private bool Send(Socket connection, byte[] buffer, int size, ref int len)
    {
        try
        {
            len = connection.Send(buffer, size, SocketFlags.None);
            return len == size;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }

    private void DeviceEventCallback(UsbmuxdDevice device, UsbmuxdConnectionEventType connectionEvent)
    {
        var udid = device.Serial;
        switch (connectionEvent)
        {
            case UsbmuxdConnectionEventType.DEVICE_ADD:
                if (!Devices.Any(d => d.Equals(udid)))
                {
                    Devices.Add(udid);
                }
                break;
            case UsbmuxdConnectionEventType.DEVICE_REMOVE:
                var value = Devices.First(d => d.Equals(udid));
                Devices.Remove(value);
                break;
            case UsbmuxdConnectionEventType.DEVICE_PAIRED:
                break;
            default:
                break;
        }
    }
    private string GetDeviceName(string udid)
    {
        using var client = MobileDevice.CreateUsingUsbmux(udid);
        return client.DeviceName;
    }
}