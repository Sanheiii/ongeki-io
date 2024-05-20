using IOConfig.Common;
using IOConfig.Helper;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.AppLifecycle;

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using Windows.ApplicationModel.Activation;
using Windows.Graphics;
using static IOConfig.Common.Win32;

using WASDK = Microsoft.WindowsAppSDK;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace IOConfig
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private static Window startupWindow;
        private static Win32WindowHelper win32WindowHelper;
        private static int registeredKeyPressedHook = 0;
        private Win32.HookProc keyEventHook;

        // Get the initial window created for this app
        // On UWP, this is simply Window.Current
        // On Desktop, multiple Windows may be created, and the StartupWindow may have already
        // been closed.
        public static Window StartupWindow
        {
            get
            {
                return startupWindow;
            }
        }
        /// <summary>
        /// Initializes the singleton Application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();

#if WINUI_PRERELEASE
            this.Suspending += OnSuspending;
            this.Resuming += App_Resuming;
            this.RequiresPointerMode = ApplicationRequiresPointerMode.WhenRequested;
#endif
        }

        public static TEnum GetEnum<TEnum>(string text) where TEnum : struct
        {
            if (!typeof(TEnum).GetTypeInfo().IsEnum)
            {
                throw new InvalidOperationException("Generic parameter 'TEnum' must be an enum.");
            }
            return (TEnum)Enum.Parse(typeof(TEnum), text);
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            startupWindow = WindowHelper.CreateWindow();
            startupWindow.ExtendsContentIntoTitleBar = true;
            startupWindow.AppWindow.Resize(new SizeInt32(600, 500));

            win32WindowHelper = new Win32WindowHelper(startupWindow);
            win32WindowHelper.SetWindowMinMaxSize(new Win32WindowHelper.POINT() { x = 600, y = 500 });


            keyEventHook = new Win32.HookProc(KeyEventHook);
            registeredKeyPressedHook = SetWindowKeyHook(keyEventHook);

            EnsureWindow();
        }

        private int KeyEventHook(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && IsKeyDownHook(lParam))
            {
                RootFrameNavigationHelper.RaiseKeyPressed((uint)wParam);
            }

            return CallNextHookEx(registeredKeyPressedHook, nCode, wParam, lParam);
        }

#if WINUI_PRERELEASE
        protected override void OnActivated(IActivatedEventArgs args)
        {
            EnsureWindow(args);
        }
#endif

        private void EnsureWindow(IActivatedEventArgs args = null)
        {
            Frame rootFrame = GetRootFrame();

            ThemeHelper.Initialize();

            Type targetPageType = typeof(HomePage);
            string targetPageArguments = string.Empty;

            NavigationRootPage rootPage = StartupWindow.Content as NavigationRootPage;
            rootPage.Navigate(targetPageType, targetPageArguments);

            // Ensure the current window is active
            StartupWindow.Activate();
        }

        public Frame GetRootFrame()
        {
            Frame rootFrame;
            NavigationRootPage rootPage = StartupWindow.Content as NavigationRootPage;
            if (rootPage == null)
            {
                rootPage = new NavigationRootPage();
                rootFrame = (Frame)rootPage.FindName("rootFrame");
                if (rootFrame == null)
                {
                    throw new Exception("Root frame not found");
                }
                SuspensionManager.RegisterFrame(rootFrame, "AppFrame");
                rootFrame.Language = Windows.Globalization.ApplicationLanguages.Languages[0];
                rootFrame.NavigationFailed += OnNavigationFailed;

                StartupWindow.Content = rootPage;
            }
            else
            {
                rootFrame = (Frame)rootPage.FindName("rootFrame");
            }

            return rootFrame;
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

#if WINUI_PRERELEASE
        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            await SuspensionManager.SaveAsync();
            deferral.Complete();
        }
#endif // WINUI_PRERELEASE

    }
}
