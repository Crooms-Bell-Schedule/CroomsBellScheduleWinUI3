namespace CroomsBellScheduleCS.UI.Views.Settings;

public sealed partial class LoginView
{
    public string Username
    {
        get
        {
            return UsernameBox.Text;
        }
    }
    public string Password
    {
        get
        {
            return PasswordBox.Password;
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

    public LoginView()
    {
        InitializeComponent();
        UsernameBox.ItemsSource = new string[] { };
    }
    private void ValidateFields()
    {
        if (string.IsNullOrEmpty(UsernameBox.Text))
        {
            LoginFailureText.Text = "A username is required.";
            return;
        }
        if (string.IsNullOrEmpty(PasswordBox.Password))
        {
            LoginFailureText.Text = "A password is required.";
            return;
        }
        LoginFailureText.Text = "";
    }
    private void UsernameBox_TextChanged(Microsoft.UI.Xaml.Controls.AutoSuggestBox sender, Microsoft.UI.Xaml.Controls.AutoSuggestBoxTextChangedEventArgs args)
    {
        ValidateFields();
    }

    private void PasswordBox_PasswordChanged(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ValidateFields();
    }
}