using CroomsBellSchedule.Service;
using CroomsBellSchedule.UI.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Win32;
using System;
using System.IO;
using System.Reflection;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using static CroomsBellSchedule.Service.SettingsManager;

namespace CroomsBellSchedule.UI.Views.Settings;

public sealed partial class PersonalizationView
{
    private bool _initialized;

    public PersonalizationView()
    {
        InitializeComponent();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        // load settings
        SetTheme(SettingsManager.Settings.Theme);

        chkTaskbar.IsOn = SettingsManager.Settings.ShowInTaskbar;
        ComboPercentage.SelectedIndex = (int)SettingsManager.Settings.PercentageSetting;
        chk1MinNotif.IsOn = !SettingsManager.Settings.Show1MinNotification;
        chk5MinNotif.IsOn = !SettingsManager.Settings.Show5MinNotification;
        chkDvd.IsOn = SettingsManager.Settings.EnableDvdScreensaver;
        chkInsider.SelectedIndex = (int)SettingsManager.Settings.UpdateChannel;
        if (chkInsider.SelectedIndex == 1 && string.IsNullOrEmpty(SettingsManager.Settings.PrivateBetaKey))
            chkInsider.SelectedIndex = 0;

            chkStartup.IsOn = GetStartup();
        UpdateCheckState();

        // show version
        var ver = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);
        VersionCard.Description = $"{ver.Major}.{ver.Minor}.{ver.Build}";

        bool updating = true;
        ToggleButton? customThemeButton = null;

        foreach (var item in Themes.ThemeList)
        {
            ToggleButton button = new()
            {
                Padding = new(2),
                Margin = new Thickness(5, 0, 5, 0)
            };

            // Check if this is the slot for the custom theme
            if (item.ID == 999)
            {
                customThemeButton = button;
                if (!string.IsNullOrEmpty(SettingsManager.Settings.CustomBackgroundPath) &&
                    File.Exists(SettingsManager.Settings.CustomBackgroundPath))
                {
                    item.BackgroundResource = SettingsManager.Settings.CustomBackgroundPath;
                    item.PreviewResource = SettingsManager.Settings.CustomBackgroundPath;
                    // TODO: more customization
                }
                else
                {
                    button.Visibility = Visibility.Collapsed;
                }
            }

            ToolTip tip = new() { Content = item.Name };
            ToolTipService.SetToolTip(button, tip);

            if (!string.IsNullOrEmpty(item.PreviewResource))
            {
                Uri uri = item.PreviewResource.Contains(":\\") ? new("file:///" + item.PreviewResource) 
                    : new Uri($"ms-appx:///Assets/Theme/" + item.PreviewResource);
                button.Content = new Image()
                {
                    Source = new BitmapImage(uri),
                    Height = 40,
                    Width = 40,
                    Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill
                };
            }

            button.Checked += async delegate (object sender, RoutedEventArgs e)
            {
                // deselect other options
                if (updating) return;

                updating = true;
                foreach (var control in ThemesContainer.Children)
                {
                    if (control is ToggleButton toggle && control != (ToggleButton)sender)
                    {
                        toggle.IsChecked = false;
                    }
                }
                updating = false;

                SettingsManager.Settings.ThemeIndex = item.ID;
                await SaveSettings();
                Themes.Apply(item.ID);
            };

            button.Unchecked += delegate (object sender, RoutedEventArgs e)
            {
                // do not allow it to be unchecked if data is not being updated
                if (updating) return;
                ((ToggleButton)sender).IsChecked = true;
            };

            // set the theme option to the one in the settings
            if (SettingsManager.Settings.ThemeIndex == item.ID)
            {
                updating = true;
                button.IsChecked = true;
                updating = false;
            }


            ThemesContainer.Children.Add(button);
        }

        // Add button
        {
            ToggleButton button = new()
            {
                Padding = new(2),
                Margin = new Thickness(5, 0, 5, 0)
            };

            ToolTip tip = new() { Content = "Add custom theme" };
            ToolTipService.SetToolTip(button, tip);


            button.Content = new SymbolIcon()
            {
                Symbol = Symbol.Add,
                Height = 40,
                Width = 40
            };

            button.Checked += async delegate (object sender, RoutedEventArgs e)
            {
                // deselect other options
                if (updating) return;

                updating = true;
                ToggleButton? oldChecked = null;
                foreach (var control in ThemesContainer.Children)
                {
                    if (control is ToggleButton toggle && control != (ToggleButton)sender)
                    {
                        if (toggle.IsChecked == true)
                            oldChecked = toggle;
                        toggle.IsChecked = false;
                    }
                }
                updating = false;

                var picker = new FileOpenPicker();
                picker.ViewMode = PickerViewMode.Thumbnail;
                picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".jpeg");
                picker.FileTypeFilter.Add(".png");
                picker.FileTypeFilter.Add(".webp");
                picker.FileTypeFilter.Add(".avif");
                picker.FileTypeFilter.Add(".bmp");

                nint windowHandle = WindowNative.GetWindowHandle(MainView.SettingsWindow);
                InitializeWithWindow.Initialize(picker, windowHandle);

                StorageFile file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    SettingsManager.Settings.CustomBackgroundPath = file.Path;
                    SettingsManager.Settings.ThemeIndex = 999;
                    await SaveSettings();

                    updating = true;
                    ((ToggleButton)sender).IsChecked = false;

                    // update custom theme preview
                    if (customThemeButton != null && customThemeButton.Content is Image img)
                    {
                        img.Source = new BitmapImage(new("file:///" + file.Path));
                        customThemeButton.IsChecked = true;
                        customThemeButton.Visibility = Visibility.Visible;
                    }
                    updating = false;
                    Themes.Apply(999);
                }
                else
                {
                    updating = true;
                    ((ToggleButton)sender).IsChecked = false;
                    oldChecked?.IsChecked = true;
                    updating = false;
                }
            };

            button.Unchecked += delegate (object sender, RoutedEventArgs e)
            {
                // do not allow it to be unchecked if data is not being updated
                if (updating) return;
                ((ToggleButton)sender).IsChecked = true;
            };


            ThemesContainer.Children.Add(button);
        }

        updating = false;

        _initialized = true;
    }

    private void UpdateCheckState()
    {
        _initialized = false;
        p5LunchA.IsChecked = SettingsManager.Settings.Period5Lunch == 0;
        p5LunchB.IsChecked = SettingsManager.Settings.Period5Lunch == 1;
        pHLunchA.IsChecked = SettingsManager.Settings.HomeroomLunch == 0;
        pHLunchB.IsChecked = SettingsManager.Settings.HomeroomLunch == 1;
        _initialized = true;
    }
    private async void ComboPercentage_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
    {
        if (!_initialized) return;

        SettingsManager.Settings.PercentageSetting = (PercentageSetting)ComboPercentage.SelectedIndex;
        await SaveSettings();

        try { MainWindow.ViewInstance.UpdateCurrentClass(); } catch { }
    }

    private async void chkTaskbar_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;

        SettingsManager.Settings.ShowInTaskbar = chkTaskbar.IsOn;

        if (chkTaskbar.IsOn && !SettingsManager.Settings.ShownTaskbarTip)
        {
            ToggleThemeTeachingTip1.IsOpen = true;
            SettingsManager.Settings.ShownTaskbarTip = true;
        }
        await SaveSettings();

        await MainWindow.ViewInstance.SetTaskbarMode(SettingsManager.Settings.ShowInTaskbar);
    }

    private async void p5LunchA_Checked(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;

        SettingsManager.Settings.Period5Lunch = 0;
        await SaveSettings();
        UpdateCheckState();

        MainWindow.ViewInstance.UpdateLunch();
    }

    private async void p5LunchB_Checked(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;

        SettingsManager.Settings.Period5Lunch = 1;
        await SaveSettings();
        UpdateCheckState();

        MainWindow.ViewInstance.UpdateLunch();
    }

    private async void pHLunchA_Checked(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;

        SettingsManager.Settings.HomeroomLunch = 0;
        await SettingsManager.SaveSettings();
        UpdateCheckState();

        MainWindow.ViewInstance.UpdateLunch();
    }

    private async void pHLunchB_Checked(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;

        SettingsManager.Settings.HomeroomLunch = 1;
        await SaveSettings();
        UpdateCheckState();

        MainWindow.ViewInstance.UpdateLunch();
    }

    private void Lunch_Unchecked(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;
        _initialized = false;
        UpdateCheckState();
        _initialized = true;
    }

    private async void ButtonCheckForUpdates_Click(object sender, RoutedEventArgs e)
    {
        ButtonCheckForUpdates.IsEnabled = false;
        await MainWindow.ViewInstance.RunUpdateCheck();
        ButtonCheckForUpdates.IsEnabled = true;
    }

    private async void chk5MinNotif_Checked(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;

        SettingsManager.Settings.Show5MinNotification = !chk5MinNotif.IsOn;
        SettingsManager.Settings.Show1MinNotification = !chk1MinNotif.IsOn;
        await SaveSettings();
    }

    internal static bool GetStartup()
    {
        RegistryKey? rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        if (rk != null && Environment.ProcessPath != null)
        {
            if (rk.GetValue("Crooms Bell Schedule App") != null)
            {
                return true;
            }
        }

        return false;
    }
    private void chkStartup_Checked(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;

        RegistryKey? rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        if (rk != null && Environment.ProcessPath != null)
        {
            if (chkTaskbar.IsOn)
                rk.SetValue("Crooms Bell Schedule App", Environment.ProcessPath);
            else
                rk.DeleteValue("Crooms Bell Schedule App", false);
        }
        else
        {
            _initialized = false;
            chkTaskbar.IsOn = false;
            _initialized = true;
        }
    }

    private CBSHColorScheme GetSelection()
    {
        return ThemeCombo.SelectedIndex switch
        {
            0 => CBSHColorScheme.Default,
            1 => CBSHColorScheme.Light,
            2 => CBSHColorScheme.Dark,
            _ => CBSHColorScheme.Default,
        };
    }

    private void SetTheme(CBSHColorScheme theme)
    {
        switch (theme)
        {
            case CBSHColorScheme.Default: ThemeCombo.SelectedIndex = 0; break;
            case CBSHColorScheme.Light: ThemeCombo.SelectedIndex = 1; break;
            case CBSHColorScheme.Dark: ThemeCombo.SelectedIndex = 2; break;
        }
    }

    private async void ThemeCombo_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
    {
        if (!_initialized) return;

        SettingsManager.Settings.Theme = GetSelection();
        await SaveSettings();

        MainWindow.ViewInstance.SetTheme(SettingsManager.Settings.Theme);
    }

    private async void chkDvd_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;

        SettingsManager.Settings.EnableDvdScreensaver = chkDvd.IsOn;
        await SaveSettings();

        // disable taskbar mode if dvd mode is enabled
        if (chkTaskbar.IsOn && chkDvd.IsOn)
        {
            chkTaskbar.IsOn = false;
        }

        MainWindow.ViewInstance.UpdateDvd();
    }

    private void Changelog_Click(object sender, RoutedEventArgs e)
    {
        MainView.Settings?.NavigateTo(typeof(WebView), new WebViewNavigationArgs("https://github.com/Crooms-Bell-Schedule/CroomsBellScheduleWinUI3/blob/master/CHANGELOG.md", true, true, false));
    }

    private void GoToSchedule_Click(object sender, RoutedEventArgs e)
    {
        MainView.Settings?.NavigateTo(typeof(BellView), new());
    }

    private async void sliderFont_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (!_initialized) return;

        SettingsManager.Settings.FontSize = sliderFont.Value;
        await SaveSettings();

        MainWindow.ViewInstance.UpdateFontSize();
    }

    private async void chkInsider_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_initialized) return;

        if (chkInsider.SelectedIndex == 1 && string.IsNullOrEmpty(SettingsManager.Settings.PrivateBetaKey))
        {
            var txt = new TextBox();
            txt.PlaceholderText = "Enter it here";
            ContentDialog dlg = new()
            {
                Title = "Enter access code. Cannot be changed after successful submission.",
                Content = txt,
                XamlRoot = XamlRoot,
                PrimaryButtonText = "Submit",
                SecondaryButtonText = "Cancel"
            };

            if (await dlg.ShowAsync() == ContentDialogResult.Primary)
            {
                var result = await Services.ApiClient.AuthenticatePrivateBeta(txt.Text);
                if (result == null)
                {
                    dlg = new ContentDialog()
                    {
                        Title = "Error",
                        XamlRoot = XamlRoot,
                        Content = "Trespass warning: Unauthorized use or access is strictly prohibited. All activity is monitored. Violators will be prosecuted to the fullest extent of the law. The Crooms Bell Schedule will continue to monitor for unauthorized access.\n\nOtherwise, check the product key.",
                        PrimaryButtonText = "Close",
                        Foreground = new SolidColorBrush(new global::Windows.UI.Color() { R = 255, B = 10, G = 10, A = 255 })
                    };
                    await dlg.ShowAsync();
                    chkInsider.SelectedIndex = (int)SettingsManager.Settings.UpdateChannel;
                    return;
                }
                else
                {
                    SettingsManager.Settings.PrivateBetaKey = result;
                }
            }
        }

        SettingsManager.Settings.UpdateChannel = (PreferredUpdateChannel)chkInsider.SelectedIndex;
        await SettingsManager.SaveSettings();

        MainView.Settings?.ShowInAppNotification("Success. Click \"Check for updates\" to install the new update(s)", "Update Channel", 10000);
    }
}