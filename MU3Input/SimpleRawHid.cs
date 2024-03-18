/* Simple Raw HID functions for C# based on http://www.pjrc.com/teensy/rawhid.html C functions (Copyright (c) 2009 PJRC.COM, LLC)
 * Copyright (c) 2017 Jan Henrik Sawatzki<jhs@sawatzki-home.de>
 *
 *  Open - open 1 or more HID devices
 *  Close - close a HID device
 *  Receive - receive a packet from HID device
 *  Send - send a packet to HID device
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above description, website URL and copyright notice and this permission
 * notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Threading;

// ReSharper disable All because it's third party source code
namespace SimpleHID.Raw
{
    class HIDDevice
    {
        public SafeFileHandle Handle;
        public bool IsOpen;
        public HIDDevice PreviousHIDDevice;
        public HIDDevice NextHIDDevice;
    }

    public class SimpleRawHID
    {
        private const int ERROR_IO_PENDING = 997;

        private const uint WAIT_OBJECT_0 = 0x00000000;
        private const uint WAIT_TIMEOUT = 0x00000102;

        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint OPEN_EXISTING = 3;

        private const uint FILE_SHARE_READ = 0x1;
        private const uint FILE_SHARE_WRITE = 0x2;
        private const uint FILE_FLAG_OVERLAPPED = 0x40000000;

        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        [StructLayout(LayoutKind.Sequential)]
        private struct HIDD_ATTRIBUTES
        {
            public int Size;
            public ushort VendorID;
            public ushort ProductID;
            public ushort VersionNumber;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVICE_INTERFACE_DATA
        {
            public int cbSize;
            public Guid interfaceClassGuid;
            public int flags;
            private UIntPtr reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HIDP_CAPS
        {
            public ushort Usage;
            public ushort UsagePage;
            public ushort InputReportByteLength;
            public ushort OutputReportByteLength;
            public ushort FeatureReportByteLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
            public ushort[] Reserved;
            public ushort NumberLinkCollectionNodes;
            public ushort NumberInputButtonCaps;
            public ushort NumberInputValueCaps;
            public ushort NumberInputDataIndices;
            public ushort NumberOutputButtonCaps;
            public ushort NumberOutputValueCaps;
            public ushort NumberOutputDataIndices;
            public ushort NumberFeatureButtonCaps;
            public ushort NumberFeatureValueCaps;
            public ushort NumberFeatureDataIndices;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        struct SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            public int cbSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
            public string DevicePath;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CRITICAL_SECTION
        {
            public IntPtr DebugInfo;
            public long LockCount;
            public long RecursionCount;
            public IntPtr OwningThread;
            public IntPtr LockSemaphore;
            public ulong SpinCount;
        }

        [Flags]
        private enum DiGetClassFlags : uint
        {
            DIGCF_DEFAULT = 0x00000001, // only valid with DIGCF_DEVICEINTERFACE
            DIGCF_PRESENT = 0x00000002,
            DIGCF_ALLCLASSES = 0x00000004,
            DIGCF_PROFILE = 0x00000008,
            DIGCF_DEVICEINTERFACE = 0x00000010,
        }

        [DllImport("hid.dll", EntryPoint = "HidD_GetHidGuid", SetLastError = true)]
        private static extern void HidD_GetHidGuid(out Guid Guid);

        [DllImport("hid.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool HidD_GetAttributes(SafeHandle DeviceObject, ref HIDD_ATTRIBUTES Attributes);

        [DllImport("hid.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool HidD_FreePreparsedData(IntPtr PreparsedData);

        [DllImport("hid.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool HidD_GetPreparsedData(SafeFileHandle hidHandle, ref IntPtr preparsedDataPointer);

        [DllImport("hid.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool HidP_GetCaps(IntPtr preparsedDataPointer, ref HIDP_CAPS capabilities);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SetupDiGetClassDevs(ref Guid ClassGuid, IntPtr Enumerator, IntPtr hwndParent, DiGetClassFlags Flags);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr hDevInfo, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData, ref SP_DEVICE_INTERFACE_DETAIL_DATA deviceInterfaceDetailData, uint deviceInterfaceDetailDataSize, IntPtr requiredSize, IntPtr deviceInfoData);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetupDiEnumDeviceInterfaces(IntPtr hDevInfo, IntPtr devInfo, ref Guid interfaceClassGuid, UInt32 memberIndex, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ReadFile(SafeFileHandle hFile, [Out] byte[] lpBuffer, uint nNumberOfBytesToRead, IntPtr lpNumberOfBytesRead, [In] ref System.Threading.NativeOverlapped lpOverlapped);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool WriteFile(SafeFileHandle hFile, byte[] lpBuffer, uint nNumberOfBytesToWrite, IntPtr lpNumberOfBytesWritten, [In] ref System.Threading.NativeOverlapped lpOverlapped);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CancelIo(IntPtr hFile);

        [DllImport("kernel32.dll")]
        private static extern void InitializeCriticalSection(out CRITICAL_SECTION lpCriticalSection);

        [DllImport("kernel32.dll")]
        private static extern void EnterCriticalSection(ref CRITICAL_SECTION lpCriticalSection);

        [DllImport("kernel32.dll")]
        private static extern void LeaveCriticalSection(ref CRITICAL_SECTION lpCriticalSection);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetOverlappedResult(IntPtr hFile, [In] ref System.Threading.NativeOverlapped lpOverlapped, out uint lpNumberOfBytesTransferred, bool bWait);

        [DllImport("kernel32.dll")]
        private static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string lpName);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ResetEvent(IntPtr hEvent);

        private static HIDDevice FirstHIDDevice = null;
        private static HIDDevice LastHIDDevice = null;

        private static IntPtr ReceiveEvent = IntPtr.Zero;
        private static IntPtr SendEvent = IntPtr.Zero;
        private static CRITICAL_SECTION ReceiveMutex = new CRITICAL_SECTION();
        private static CRITICAL_SECTION SendMutex = new CRITICAL_SECTION();

        public SimpleRawHID()
        {
        }

        //  Open - opens 1 or more HID devices
        //
        //  Inputs:
        //      max = maximum number of devices to open
        //      vid = Vendor ID, or -1 if any
        //      pid = Product ID, or -1 if any
        //      usagePage = top level usage page, or -1 if any
        //      usage = top level usage number, or -1 if any
        //  Output:
        //      actual number of devices opened
        //
        public int Open(int max, uint vid, uint pid, int usagePage = -1, int usage = -1)
        {
            if (FirstHIDDevice != null)
            {
                FreeAllHIDDevices();
            }

            if (max < 1)
            {
                return 0;
            }
            if (ReceiveEvent == IntPtr.Zero)
            {
                ReceiveEvent = CreateEvent(IntPtr.Zero, true, true, null);
                SendEvent = CreateEvent(IntPtr.Zero, true, true, null);
                InitializeCriticalSection(out ReceiveMutex);
                InitializeCriticalSection(out SendMutex);
            }

            Guid guid;
            HidD_GetHidGuid(out guid);
            IntPtr info = SetupDiGetClassDevs(ref guid, IntPtr.Zero, IntPtr.Zero, DiGetClassFlags.DIGCF_PRESENT | DiGetClassFlags.DIGCF_DEVICEINTERFACE);
            if (info == INVALID_HANDLE_VALUE)
            {
                return 0;
            }

            uint index = 0;
            int deviceCount = 0;
            bool retValue;

            while (true)
            {
                SP_DEVICE_INTERFACE_DATA iface = new SP_DEVICE_INTERFACE_DATA();
                iface.cbSize = Marshal.SizeOf(iface);

                retValue = SetupDiEnumDeviceInterfaces(info, IntPtr.Zero, ref guid, index, ref iface);
                if (!retValue)
                {
                    SetupDiDestroyDeviceInfoList(info);
                    return deviceCount;
                }
                SP_DEVICE_INTERFACE_DETAIL_DATA details = new SP_DEVICE_INTERFACE_DETAIL_DATA();
                if (IntPtr.Size == 8)
                {
                    details.cbSize = 8; // for 64 bit os
                }

                else
                {
                    details.cbSize = 4 + Marshal.SystemDefaultCharSize; // for 32 bit os
                }

                retValue = SetupDiGetDeviceInterfaceDetail(info, ref iface, ref details, 1024, IntPtr.Zero, IntPtr.Zero);
                if (!retValue)
                {
                    index++;
                    continue;
                }

                SafeFileHandle h = CreateFile(details.DevicePath, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, FILE_FLAG_OVERLAPPED, IntPtr.Zero);

                if (h.IsInvalid)
                {
                    index++;
                    continue;
                }
                HIDD_ATTRIBUTES attrib = new HIDD_ATTRIBUTES();
                attrib.Size = Marshal.SizeOf(attrib);
                retValue = HidD_GetAttributes(h, ref attrib);

                IntPtr hidData = new IntPtr(0);
                // Console.WriteLine($"{index} - {attrib.VendorID:x}:{attrib.ProductID:x}");

                if (!retValue || (vid > 0 && attrib.VendorID != vid) ||
                    (pid > 0 && attrib.ProductID != pid) ||
                    !HidD_GetPreparsedData(h, ref hidData))
                {
                    h.SetHandleAsInvalid();
                    h.Dispose();
                    h = null;
                    index++;
                    continue;
                }

                HIDP_CAPS capabilities = new HIDP_CAPS();

                retValue = HidP_GetCaps(hidData, ref capabilities);

                if (!retValue || (usagePage > 0 && capabilities.UsagePage != usagePage) ||
                    (usage > 0 && capabilities.Usage != usage))
                {
                    HidD_FreePreparsedData(hidData);
                    h.SetHandleAsInvalid();
                    h.Dispose();
                    h = null;
                    index++;
                    continue;
                }
                HidD_FreePreparsedData(hidData);
                HIDDevice device = new HIDDevice();
                device.Handle = h;
                device.IsOpen = true;

                AddHIDDevice(device);
                deviceCount++;
                if (deviceCount >= max)
                {
                    SetupDiDestroyDeviceInfoList(info);
                    return deviceCount;
                }
                index++;
            }
        }

        //  Close - closes a HID device
        //
        //  Inputs:
        //      (nothing)
        //  Output
        //      (nothing)
        //
        public void Close()
        {
            if (FirstHIDDevice != null)
            {
                FreeAllHIDDevices();
            }
        }

        //  Receive - receives a packet
        //
        //  Inputs:
        //      num = device to receive from (zero based)
        //      buf = buffer to receive packet
        //      len = buffer's size
        //      timeout = time to wait, in milliseconds
        //  Output:
        //      number of bytes received, or -1 on error
        //
        public int Receive(int num, ref byte[] buf, int len, int timeout)
        {
            byte[] tempBuffer = new byte[516];

            if (tempBuffer.Length < len + 1)
            {
                return -1;
            }

            HIDDevice device = GetHIDDevice(num);
            if (device == null || !device.IsOpen)
            {
                return -1;
            }

            EnterCriticalSection(ref ReceiveMutex);
            ResetEvent(ReceiveEvent);
            NativeOverlapped overlapped = new NativeOverlapped();
            overlapped.EventHandle = ReceiveEvent;
            if (!ReadFile(device.Handle, tempBuffer, (uint)len + 1, IntPtr.Zero, ref overlapped))
            {
                if (Marshal.GetLastWin32Error() != ERROR_IO_PENDING)
                {
                    LeaveCriticalSection(ref ReceiveMutex);
                    return -1;
                }

                uint waitStatus = WaitForSingleObject(ReceiveEvent, (uint)timeout);
                if (waitStatus == WAIT_TIMEOUT)
                {
                    CancelIo(device.Handle.DangerousGetHandle());
                    LeaveCriticalSection(ref ReceiveMutex);
                    return 0;
                }
                if (waitStatus != WAIT_OBJECT_0)
                {
                    LeaveCriticalSection(ref ReceiveMutex);
                    return -1;
                }
            }

            uint numberOfBytesReceived;
            if (!GetOverlappedResult(device.Handle.DangerousGetHandle(), ref overlapped, out numberOfBytesReceived, false))
            {
                LeaveCriticalSection(ref ReceiveMutex);
                return -1;
            }

            LeaveCriticalSection(ref ReceiveMutex);

            if (numberOfBytesReceived <= 0)
            {
                return -1;
            }

            numberOfBytesReceived--;

            if (numberOfBytesReceived > len)
            {
                numberOfBytesReceived = (uint)len;
            }

            Array.Copy(tempBuffer, 1, buf, 0, numberOfBytesReceived);
            return (int)numberOfBytesReceived;
        }

        //  Send - sends a packet
        //
        //  Inputs:
        //      num = device to transmit to (zero based)
        //      buf = buffer containing packet to send
        //      len = number of bytes to transmit
        //      timeout = time to wait, in milliseconds
        //  Output:
        //      number of bytes sent, or -1 on error
        //
        public int Send(int num, byte report_id, byte[] buf, int len, int timeout)
        {
            byte[] tempBuffer = new byte[516];

            if (tempBuffer.Length < len + 1)
            {
                return -1;
            }

            HIDDevice device = GetHIDDevice(num);
            if (device == null || !device.IsOpen)
            {
                return -1;
            }
            EnterCriticalSection(ref SendMutex);
            ResetEvent(SendEvent);

            NativeOverlapped overlapped = new NativeOverlapped();
            overlapped.EventHandle = SendEvent;

            tempBuffer[0] = report_id;
            Array.Copy(buf, 0, tempBuffer, 1, len);

            if (!WriteFile(device.Handle, tempBuffer, (uint)len + 1, IntPtr.Zero, ref overlapped))
            {
                var err = Marshal.GetLastWin32Error();
                if (err != ERROR_IO_PENDING)
                {
                    LeaveCriticalSection(ref SendMutex);
                    return -1;
                }

                uint waitStatus = WaitForSingleObject(ReceiveEvent, (uint)timeout);
                if (waitStatus == WAIT_TIMEOUT)
                {
                    CancelIo(device.Handle.DangerousGetHandle());
                    LeaveCriticalSection(ref SendMutex);
                    return 0;
                }
                if (waitStatus != WAIT_OBJECT_0)
                {
                    LeaveCriticalSection(ref SendMutex);
                    return -1;
                }
            }

            uint numberOfBytesSend;
            if (!GetOverlappedResult(device.Handle.DangerousGetHandle(), ref overlapped, out numberOfBytesSend, false))
            {
                LeaveCriticalSection(ref SendMutex);
                return -1;
            }

            LeaveCriticalSection(ref SendMutex);

            if (numberOfBytesSend <= 0)
            {
                return -1;
            }

            return (int)numberOfBytesSend - 1;
        }

        private void AddHIDDevice(HIDDevice device)
        {
            if (FirstHIDDevice == null || LastHIDDevice == null)
            {
                FirstHIDDevice = device;
                LastHIDDevice = device;

                device.NextHIDDevice = null;
                device.PreviousHIDDevice = null;

                return;
            }

            LastHIDDevice.NextHIDDevice = device;
            device.PreviousHIDDevice = LastHIDDevice;
            device.NextHIDDevice = null;
            LastHIDDevice = device;
        }

        private HIDDevice GetHIDDevice(int num)
        {
            HIDDevice p = FirstHIDDevice;
            while (p != null && num > 0)
            {
                p = p.NextHIDDevice;
                num--;
            }
            return p;
        }

        private void FreeAllHIDDevices()
        {
            HIDDevice p = FirstHIDDevice;
            HIDDevice q = null;

            while (p != null)
            {
                q = p;
                CloseHIDDevice(p);
                p = p.NextHIDDevice;
                q = null;
            }

            FirstHIDDevice = null;
            LastHIDDevice = null;
        }

        private void CloseHIDDevice(HIDDevice device)
        {
            if (device.IsOpen)
            {
                device.Handle.SetHandleAsInvalid();
                device.Handle.Dispose();
                device.Handle = null;
            }
        }
    }
}
