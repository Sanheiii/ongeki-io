using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace IOConfig
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class EditScopeDialogContent : Page
    {
        private bool initialized = false;
        public List<Scope> Items { get; private set; }
        public Scope Scope { get; set; }
        public EditScopeDialogContent(Scope scope)
        {
            Items =
            [
                Scope.L1,
                Scope.L2,
                Scope.L3,
                Scope.LSide,
                Scope.LMenu,
                Scope.R1,
                Scope.R2,
                Scope.R3,
                Scope.RSide,
                Scope.RMenu,
                Scope.Lever,
                Scope.Aime
            ];
            DataContext = this;
            this.InitializeComponent();

            ScopeList.SelectedIndex = 0;
            Scope = scope;
        }

        private void ScopeList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var scopeList = (ListView)sender;
            foreach (var item in e.RemovedItems)
            {
                scopeList.SelectedItems.Remove(item);
            }
            if (!initialized)
            {
                initialized = true;
                var scope = Scope;
                ScopeList.SelectedItems.Clear();
                foreach (var item in Items)
                {
                    if (scope.HasFlag(item)) ScopeList.SelectedItems.Add(item);
                }
                Console.WriteLine(0);
            }
            if (scopeList.SelectedItems.Count < 1)
            {
                Scope = Scope.None;
                return;
            }
            var scopes = scopeList.SelectedItems.Cast<Scope>().ToArray();
            Scope = scopes.Aggregate((l, r) => l | r);
        }
    }
}
