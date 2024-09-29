using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Data;

using Windows.System;
using ABI.Microsoft.UI;
using ABI.Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Color = Windows.UI.Color;
using Colors = Microsoft.UI.Colors;

namespace IOConfig
{
    public class KeyCodeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int keyCode)
            {
                if(keyCode < 0) return string.Empty;
                return Enum.GetName(typeof(VirtualKey), keyCode) ?? Enum.GetName(typeof(ConsoleKey), keyCode) ?? keyCode.ToString();
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is not string str) return -1;
            if (Enum.GetNames<VirtualKey>().Contains(str)) return (int)Enum.Parse<VirtualKey>(str);
            if (Enum.GetNames<ConsoleKey>().Contains(str)) return (int)Enum.Parse<ConsoleKey>(str);
            if (int.TryParse(str, out var num)) return num;
            return -1;
        }
    }
    public class VisualButtonMaskVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is byte v && parameter is string so && int.TryParse(so, out int o))
            {
                int mask = 1 << o;
                if ((v & mask) > 0) return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException("Dan't set button status");
        }
    }
    public class VisualButtonPressedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is byte v && parameter is string so && int.TryParse(so, out int o))
            {
                int mask = 1 << o;
                if ((v & mask) > 0) return true;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException("Dan't set button status");
        }
    }
    public class StatusTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is true) return "已连接";
            return "未连接";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException("Dan't set button status");
        }
    }
    public class StatusBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is true) return new SolidColorBrush(Colors.Green);
            return new SolidColorBrush(Colors.Red);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException("Dan't set button status");
        }
    }
    public abstract class IntegerConvetcer<T> : IValueConverter where T : INumber<T>
    {
        public abstract T DefaultValue { get; }
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (T.TryParse(value.ToString(), NumberStyles.Integer, null, out T result))
            {
                return result;
            };
            return DefaultValue;
        }
    }

    public class Int32Converter : IntegerConvetcer<int>
    {
        public override int DefaultValue => -1;
    }
    public class UInt32Converter : IntegerConvetcer<uint>
    {
        public override uint DefaultValue => 0;
    }
    public class UInt16Converter : IntegerConvetcer<ushort>
    {
        public override ushort DefaultValue => 0;
    }

}
