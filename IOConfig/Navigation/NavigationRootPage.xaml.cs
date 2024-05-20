//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************
using IOConfig.Helper;

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Windows.System.Profile;
using Windows.UI.ViewManagement;

namespace IOConfig
{
    public sealed partial class NavigationRootPage : Page
    {
        public Windows.System.VirtualKey ArrowKey;
        public Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue;
        private RootFrameNavigationHelper _navHelper;
        private UISettings _settings;


        public static NavigationRootPage GetForElement(object obj)
        {
            UIElement element = (UIElement)obj;
            Window window = WindowHelper.GetWindowForElement(element);
            if (window != null)
            {
                return (NavigationRootPage)window.Content;
            }
            return null;
        }

        public Microsoft.UI.Xaml.Controls.NavigationView NavigationView
        {
            get { return NavigationViewControl; }
        }

        public Action NavigationViewLoaded { get; set; }

        public DeviceType DeviceFamily { get; set; }

        public string AppTitleText
        {
            get
            {
#if DEBUG
                return "Mu3 IO Config Dev";
#else
                return "Mu3 IO Config";
#endif
            }
        }

        public NavigationRootPage()
        {
            this.InitializeComponent();
            dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

            _navHelper = new RootFrameNavigationHelper(rootFrame, NavigationViewControl);

            SetDeviceFamily();
            //AddNavigationMenuItems();

            this.GotFocus += (object sender, RoutedEventArgs e) =>
            {
                // helpful for debugging focus problems w/ keyboard & gamepad
                if (FocusManager.GetFocusedElement() is FrameworkElement focus)
                {
                    Debug.WriteLine("got focus: " + focus.Name + " (" + focus.GetType().ToString() + ")");
                }
            };

            // remove the solid-colored backgrounds behind the caption controls and system back button if we are in left mode
            // This is done when the app is loaded since before that the actual theme that is used is not "determined" yet
            Loaded += delegate (object sender, RoutedEventArgs e)
            {
                Window window = WindowHelper.GetWindowForElement(sender as UIElement);
                window.Title = AppTitleText;
                window.ExtendsContentIntoTitleBar = true;
                window.Activated += Window_Activated;
                window.SetTitleBar(this.AppTitleBar);

                AppWindow appWindow = WindowHelper.GetAppWindow(window);
                appWindow.SetIcon("Assets/Tiles/GalleryIcon.ico");
                _settings = new UISettings();
                _settings.ColorValuesChanged += _settings_ColorValuesChanged; // cannot use FrameworkElement.ActualThemeChanged event because the triggerTitleBarRepaint workaround no longer works
            };
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState == WindowActivationState.Deactivated)
            {
                VisualStateManager.GoToState(this, "Deactivated", true);
            }
            else
            {
                VisualStateManager.GoToState(this, "Activated", true);
            }
        }

        // this handles updating the caption button colors correctly when indows system theme is changed
        // while the app is open
        private void _settings_ColorValuesChanged(UISettings sender, object args)
        {
            // This calls comes off-thread, hence we will need to dispatch it to current app's thread
            dispatcherQueue.TryEnqueue(() =>
            {
                _ = TitleBarHelper.ApplySystemThemeToCaptionButtons(App.StartupWindow);
            });
        }

        // Wraps a call to rootFrame.Navigate to give the Page a way to know which NavigationRootPage is navigating.
        // Please call this function rather than rootFrame.Navigate to navigate the rootFrame.
        public void Navigate(
            Type pageType,
            object targetPageArguments = null,
            Microsoft.UI.Xaml.Media.Animation.NavigationTransitionInfo navigationTransitionInfo = null)
        {
            NavigationRootPageArgs args = new NavigationRootPageArgs();
            args.NavigationRootPage = this;
            args.Parameter = targetPageArguments;
            rootFrame.Navigate(pageType, args, navigationTransitionInfo);
        }

        private void SetDeviceFamily()
        {
            var familyName = AnalyticsInfo.VersionInfo.DeviceFamily;

            if (!Enum.TryParse(familyName.Replace("Windows.", string.Empty), out DeviceType parsedDeviceType))
            {
                parsedDeviceType = DeviceType.Other;
            }

            DeviceFamily = parsedDeviceType;
        }
    }

    public class NavigationRootPageArgs
    {
        public NavigationRootPage NavigationRootPage;
        public object Parameter;
    }

    public enum DeviceType
    {
        Desktop,
        Mobile,
        Other,
        Xbox
    }
}
