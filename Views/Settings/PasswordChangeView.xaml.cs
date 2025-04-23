namespace CroomsBellScheduleCS.Views.Settings;

public sealed partial class PasswordChangeView
{
    public string PasswordOld
    {
        get
        {
            return Password1.Password;
        }
    }
    public string PasswordNew
    {
        get
        {
            return Password2.Password;
        }
    }

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
            LoginFailureText.Text = value;
        }
    }

    public PasswordChangeView()
    {
        InitializeComponent();
    }
    public bool ValidateFields()
    {
        if (string.IsNullOrEmpty(Password1.Password))
        {
            LoginFailureText.Text = "Old password is required";
            return false;
        }
        if (string.IsNullOrEmpty(Password2.Password))
        {
            LoginFailureText.Text = "New password is required";
            return false;
        }
        if (Password2.Password != Password3.Password)
        {
            LoginFailureText.Text = "New password is required";
            return false;
        }

        LoginFailureText.Text = "";
        return true;
    }

    private void PasswordBox_PasswordChanged(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ValidateFields();
    }
}