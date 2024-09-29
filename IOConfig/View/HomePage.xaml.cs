using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json.Nodes;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using IOConfig.Helper;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace IOConfig
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HomePage : Page
    {
        HomeViewModel ViewModel => DataContext as HomeViewModel;
        public HomePage()
        {
            this.InitializeComponent();
            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        private void CompositionTarget_Rendering(object sender, object e)
        {
            ViewModel.UpdateStates();
        }

        private async void AddController_OnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog();
            var dialogContent = new AddControllerDialogContent();

            dialog.XamlRoot = XamlRoot;
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.Title = "选择控制器类型";
            dialog.PrimaryButtonText = "确定";
            dialog.CloseButtonText = "取消";
            dialog.DefaultButton = ContentDialogButton.Primary;
            dialog.Content = dialogContent;

            try
            {
                if (await dialog.ShowAsync() is not ContentDialogResult.Primary) return;
            }
            catch {return;}

            var controllerType = dialogContent.SelectedItem;

            if (controllerType is IOType.Udp) ViewModel.IOConfigs.Add(new ConfigItem
            {
                Type = IOType.Udp,
                Param = JsonValue.Create((ushort)4354),
                Scope = Scope.All
            });
            else if (controllerType is IOType.Tcp) ViewModel.IOConfigs.Add(new ConfigItem
            {
                Type = IOType.Tcp,
                Param = JsonValue.Create((ushort)4354),
                Scope = Scope.All
            });
            else if (controllerType is IOType.Usbmux) ViewModel.IOConfigs.Add(new ConfigItem
            {
                Type = IOType.Usbmux,
                Param = JsonValue.Create((ushort)4354),
                Scope = Scope.All
            });
            else if (controllerType is IOType.Keyboard) ViewModel.IOConfigs.Add(new ConfigItem
            {
                Type = IOType.Keyboard,
                Param = new KeyboardParam(),
                Scope = Scope.All
            });
            else if (controllerType is IOType.Hid) ViewModel.IOConfigs.Add(new ConfigItem
            {
                Type = IOType.Hid,
                Param = new HidParam(),
                Scope = Scope.All
            });
        }

        private async void EditScope_OnClick(object sender, RoutedEventArgs e)
        {
            var context = (ConfigItem)((FrameworkElement)sender).DataContext;
            var dialog = new ContentDialog();
            var dialogContent = new EditScopeDialogContent(context.Scope);

            dialog.XamlRoot = XamlRoot;
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.Title = "选择作用域";
            dialog.PrimaryButtonText = "确定";
            dialog.CloseButtonText = "取消";
            dialog.DefaultButton = ContentDialogButton.Primary;
            dialog.Content = dialogContent;

            if (await dialog.ShowAsync() is not ContentDialogResult.Primary) return;

            context.Scope = dialogContent.Scope;
        }

        private void KeyboardInputSetting_OnClick(object sender, RoutedEventArgs e)
        {
            var element = (FrameworkElement)sender;
            NavigationRootPage rootPage = App.StartupWindow.Content as NavigationRootPage;
            rootPage.Navigate(typeof(KeyboardInputSettingPage), element.DataContext);
        }

        private void Lever_OnHolding(object sender, HoldingRoutedEventArgs e)
        {
            e.Handled = true;
        }
    }
}
