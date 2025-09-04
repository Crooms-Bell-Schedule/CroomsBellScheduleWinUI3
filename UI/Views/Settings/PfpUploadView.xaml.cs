using System;
using Windows.Storage.Pickers;
using Windows.Storage;
using WinRT.Interop;
using CroomsBellScheduleCS.UI.Windows;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml.Controls;

namespace CroomsBellScheduleCS.UI.Views.Settings;

public sealed partial class PfpUploadView
{
    public ImageCropper Cropper { get => cropper; }
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
            ErrorText.Text = value;
        }
    }

    public PfpUploadView()
    {
        InitializeComponent();
    }

    public void SetMode(UploadViewMode mode)
    {
        switch (mode)
        {
            case UploadViewMode.ProfilePicture:
                cropper.AspectRatio = 1d / 1d;
                cropper.CropShape = CropShape.Circular;
                break;
            case UploadViewMode.ProfileBanner:
                cropper.AspectRatio = 4d / 1d;
                cropper.CropShape = CropShape.Rectangular;
                break;
            default:
                break;
        }
    }

    private async void SelectButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            FileOpenPicker fileOpenPicker = new()
            {
                ViewMode = PickerViewMode.Thumbnail,
                FileTypeFilter = { ".jpg", ".jpeg", ".png", ".gif", ".webp" },
            };

            nint windowHandle = WindowNative.GetWindowHandle(MainWindow.Instance);
            InitializeWithWindow.Initialize(fileOpenPicker, windowHandle);

            StorageFile file = await fileOpenPicker.PickSingleFileAsync();

            if (file != null)
            {
                await cropper.LoadImageFromFile(file);
                ErrorText.Text = "";
            }
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
        }
    }

    public enum UploadViewMode
    {
        ProfilePicture,
        ProfileBanner
    }
}