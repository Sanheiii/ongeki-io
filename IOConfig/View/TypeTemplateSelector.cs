using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace IOConfig
{
    public class TypeTemplateSelector : DataTemplateSelector
    {
        public DataTemplate HidTemplate { get; set; }
        public DataTemplate UdpTemplate { get; set; }
        public DataTemplate TcpTemplate { get; set; }
        public DataTemplate UsbmuxTemplate { get; set; }
        public DataTemplate KeyboardTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is ConfigItem ioData)
            {
                return ioData.Type switch
                {
                    IOType.Hid => HidTemplate,
                    IOType.Udp => UdpTemplate,
                    IOType.Tcp => TcpTemplate,
                    IOType.Usbmux => UsbmuxTemplate,
                    IOType.Keyboard => KeyboardTemplate,
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
            return base.SelectTemplateCore(item, container);
        }
    }
}
