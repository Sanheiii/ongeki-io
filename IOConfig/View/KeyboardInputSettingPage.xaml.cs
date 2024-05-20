using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace IOConfig
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class KeyboardInputSettingPage : Page
    {
        public KeyboardInputSettingPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is NavigationRootPageArgs args && args.Parameter is ConfigItem io)
            {
                DataContext = io.Param;
            }
        }

        private void KeyInputTextBox_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var textBox = (TextBox)sender;
            textBox.Focus(FocusState.Programmatic);
        }

        private void KeyInputTextBox_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            int virtualKeyCode = -1;
            if (e.Key is VirtualKey.Shift or VirtualKey.Menu or VirtualKey.Control)
            {
                var scanCode = e.KeyStatus.ScanCode;
                virtualKeyCode = (int)User32.MapVirtualKeyEx(scanCode, 3, IntPtr.Zero);
            }
            else virtualKeyCode = (int)e.OriginalKey;
            
            var keyName = Enum.GetName(typeof(VirtualKey), virtualKeyCode) ?? Enum.GetName(typeof(ConsoleKey), e.Key) ?? e.Key.ToString();
            var textBox = (TextBox)sender;
            if (e.Key is VirtualKey.Escape)
                textBox.Text = string.Empty;
            else
                textBox.Text = keyName;
            FocusHolder.Focus(FocusState.Programmatic);
            e.Handled = true;
        }

        private void KeyInputTextBox_OnGotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox)sender;
            textBox.PlaceholderText = "°´ÏÂ";
            textBox.Text = string.Empty;
        }

        private void KeyInputTextBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox)sender;
            textBox.PlaceholderText = "ÎÞ";
        }

        private void Page_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            FocusHolder.Focus(FocusState.Programmatic);
        }
    }
}
