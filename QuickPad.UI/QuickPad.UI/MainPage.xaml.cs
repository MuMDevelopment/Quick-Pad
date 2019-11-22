﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Core.Preview;
using QuickPad.UI.Common;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Store;
using Windows.Graphics.Display;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.ViewManagement;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.WindowManagement;
using Microsoft.Extensions.Logging;
using QuickPad.Mvc;
using QuickPad.Mvvm;
using QuickPad.Mvvm.Commands;
using QuickPad.Mvvm.ViewModels;
using Windows.System;
using QuickPad.UI.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace QuickPad.UI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, IDocumentView
    {
        public VisualThemeSelector VisualThemeSelector { get; } = VisualThemeSelector.Default;
        public SettingsViewModel Settings => App.Settings;
        public QuickPadCommands Commands => App.Commands;
        private ILogger<MainPage> Logger { get; }

        public MainPage(ILogger<MainPage> logger, DocumentViewModel viewModel)
        {
            Logger = logger;
            ViewModel = viewModel;

            App.Controller.AddView(this);
            Initialize?.Invoke(this, Commands);

            this.InitializeComponent();
            DataContext = ViewModel;

            Loaded += OnLoaded;

            //extent app in to the title bar
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            
            var tBar = ApplicationView.GetForCurrentView().TitleBar;
            tBar.ButtonBackgroundColor = Colors.Transparent;
            tBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            ViewModel.Document = RichEditBox.Document;
            RichEditBox.TextChanged += ViewModel.TextChanged;
            
            ViewModel.ExitApplication = ExitApp;

            ViewModel.PropertyChanged += ViewModel_PropertyChanged;

            Settings.PropertyChanged += Settings_PropertyChanged;

            SystemNavigationManagerPreview.GetForCurrentView().CloseRequested += this.OnCloseRequest;

            var currentView = SystemNavigationManager.GetForCurrentView();
            currentView.BackRequested += CurrentView_BackRequested;

            commandBar.SetFontName += CommandBarOnSetFontName;
            commandBar.SetFontSize += CommandBarOnSetFontSize;

        }

        private void CommandBarOnSetFontSize(double fontSize)
        {
            RichEditBox.FontSize = fontSize;
        }

        private void CommandBarOnSetFontName(string fontFamilyName)
        {
            RichEditBox.FontFamily = new FontFamily(fontFamilyName);
        }

        private void CurrentView_BackRequested(object sender, BackRequestedEventArgs e)
        {
            var newMode = Settings.DefaultMode == DisplayModes.LaunchFocusMode.ToString() 
                ? DisplayModes.LaunchClassicMode.ToString()
                : Settings.DefaultMode;

            SetOverlayMode(newMode)
                .ContinueWith(_ => { Settings.CurrentMode = newMode; });
        }

        public StorageFile FileToLoad { get; set; }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            SetupFocusMode(Settings.FocusMode);
            
            await SetOverlayMode(Settings.CurrentMode);
            await ViewModel.InitNewDocument();

            Mvvm.ViewModels.ViewModel.Dispatch(() => Bindings.Update());

            if (Settings.DefaultMode == "Compact Overlay")
            {
                if(await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay))
                {
                    Settings.CurrentMode = "Compact Overlay";
                }
            }

            Settings.NotDeferred = true;
            Settings.Status("Ready", TimeSpan.FromSeconds(10), SettingsViewModel.Verbosity.Release);

            if (FileToLoad != null && LoadFromFile != null)
            {
                await LoadFromFile(ViewModel, FileToLoad);
                FileToLoad = null;
            }
        }

        public event Func<DocumentViewModel, StorageFile, Task> LoadFromFile;

        private async Task SetOverlayMode(string mode)
        {
            if (mode == DisplayModes.LaunchCompactOverlay.ToString())
            {
                if (await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay))
                {
                    Settings.CurrentMode = "Compact Overlay";
                }
            }
            else
            {
                if (await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.Default))
                {
                    Settings.CurrentMode = Settings.PreviousMode;
                }
            }
        }

        private void ExitApp()
        {
            CoreApplication.Exit();
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Bindings.Update();
            switch (e.PropertyName)
            {
                case nameof(Settings.CurrentMode):
                    SetupFocusMode(Settings.FocusMode);
                    break;
            }
        }

        private void SetupFocusMode(bool enabled)
        {
            var currentView = SystemNavigationManager.GetForCurrentView();
            if (enabled)
            {
                currentView.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
                var di = DisplayInformation.GetForCurrentView();
                Settings.BackButtonWidth = 48.0 * ((double)di.ResolutionScale / 100.0);
            }
            else
            {
                currentView.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
            }
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ViewModel.Title):
                    var appView = ApplicationView.GetForCurrentView();
                    appView.Title = ViewModel.Title;
                    break;

                case nameof(ViewModel.Text):
                    Commands.NotifyChanged(ViewModel, Settings);
                    break;
            }

            try
            {
                Bindings.Update();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error binding objects.");
            }
            
        }

        private async void OnCloseRequest(object sender, SystemNavigationCloseRequestedPreviewEventArgs e)
        {
            ViewModel.Deferral = e.GetDeferral();

            if (ExitApplication == null) ViewModel.Deferral.Complete();
            else
            {
                e.Handled = !(await ExitApplication(ViewModel));
                
                if (!e.Handled) return;

                try
                {
                    ViewModel.Deferral?.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    Logger.LogDebug("Handled Deferral already disposed.");
                }
            }
        }

        public DocumentViewModel ViewModel { get; set; }
        public event Action<IDocumentView, QuickPadCommands> Initialize;
        public event Func<DocumentViewModel, Task<bool>> ExitApplication;

        private void ChangeModeByKey(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
        }

        private async void MainPage_OnKeyUp(object sender, KeyRoutedEventArgs args)
        {
            var controlDown = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control)
                .HasFlag(CoreVirtualKeyStates.Down);
            var menuDown = Window.Current.CoreWindow.GetKeyState(VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down);
            var shiftDown = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift)
                .HasFlag(CoreVirtualKeyStates.Down);
            var leftWindowsDown = Window.Current.CoreWindow.GetKeyState(VirtualKey.LeftWindows)
                .HasFlag(CoreVirtualKeyStates.Down);
            var rightWindowsDown = Window.Current.CoreWindow.GetKeyState(VirtualKey.RightWindows)
                .HasFlag(CoreVirtualKeyStates.Down);

            var option = (c: controlDown, s: shiftDown, m: menuDown, l: leftWindowsDown, r: rightWindowsDown, k: args.Key);

            var newMode = option switch
            {
                (true, true, false, false, false, VirtualKey.Number1) => DisplayModes.LaunchClassicMode.ToString(),
                (true, true, false, false, false, VirtualKey.Number2) => DisplayModes.LaunchDefaultMode.ToString(),
                (false, false, true, false, false, VirtualKey.Up) => DisplayModes.LaunchCompactOverlay.ToString(),
                (false, false, true, false, false, VirtualKey.Down) => Settings.CurrentMode == DisplayModes.LaunchCompactOverlay.ToString() ? Settings.DefaultMode : Settings.CurrentMode,
                (true, true, false, false, false, VirtualKey.Number4) => DisplayModes.LaunchFocusMode.ToString(),
                (true, false, false, false, false, VirtualKey.F12) => DisplayModes.LaunchNinjaMode.ToString(),
                (false, false, false, false, false, VirtualKey.Escape) => Settings.DefaultMode,
                _ => Settings.CurrentMode
            };

            if (Settings.CurrentMode == newMode) return;

            SetupFocusMode(newMode == DisplayModes.LaunchFocusMode.ToString());
            
            await SetOverlayMode(newMode);

            Settings.CurrentMode = newMode;

            Settings.Status($"{Settings.CurrentModeText} Enabled.", TimeSpan.FromSeconds(5), SettingsViewModel.Verbosity.Debug);

        }

        private void RichEditBox_OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
        }

        private void TextBox_OnGotFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                Bindings.Update();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error updating binding on MainPage.");
            }
        }
    }
}
