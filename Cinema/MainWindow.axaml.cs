using Avalonia.Controls;

namespace Cinema;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ShowLoginPage();
    }

    public void ShowLoginPage()
    {
        MainContent.Content = new Pages.LoginPage(this);
    }

    public void ShowRegisterPage()
    {
        MainContent.Content = new Pages.RegisterPage(this);
    }

    public void ShowMainPage()
    {
        MainContent.Content = new Pages.MainPage(this);
    }

    public void ShowProfilePage()
    {
        MainContent.Content = new Pages.ProfilePage(this);
    }
}
