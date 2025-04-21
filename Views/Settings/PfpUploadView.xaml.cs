using System;
using Windows.Storage.Pickers;
using Windows.Storage;
using WinRT.Interop;
using CroomsBellScheduleCS.Windows;
using CommunityToolkit.WinUI.Controls;

namespace CroomsBellScheduleCS.Views.Settings;

public sealed partial class PfpUploadView
{
    public bool ShowingLoading
    {
        get
        {
            return ContentArea.Visibility == Microsoft.UI.Xaml.Visibility.Visible;
        }
        set
        {
            ContentArea.Visibility = value ? Microsoft.UI.Xaml.Visibility.Collapsed : Microsoft.UI.Xaml.Visibility.Visible;
            Loader.Visibility = value ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
        }
    }

    public string Error
    {
        set
        {
            //LoginFailureText.Text = value;
        }
    }

    public PfpUploadView()
    {
        InitializeComponent();
    }
    private void ValidateFields()
    {
        
    }

    private async void SelectButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            cropper.AspectRatio = 1d / 1d;
            FileOpenPicker fileOpenPicker = new()
            {
                ViewMode = PickerViewMode.Thumbnail,
                FileTypeFilter = { ".jpg", ".jpeg", ".png", ".gif" },
            };

            nint windowHandle = WindowNative.GetWindowHandle(MainWindow.Instance);
            InitializeWithWindow.Initialize(fileOpenPicker, windowHandle);

            StorageFile file = await fileOpenPicker.PickSingleFileAsync();

            if (file != null)
            {
                await cropper.LoadImageFromFile(file);
            }
        }
        catch
        {

        }
    }
}