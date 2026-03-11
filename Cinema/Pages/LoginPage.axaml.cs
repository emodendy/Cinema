using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Cinema.Authentication;

namespace Cinema.Pages;

public partial class LoginPage : UserControl
{
    private readonly MainWindow _mainWindow;

    public LoginPage(MainWindow mainWindow)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
    }

    private void OnLoginClick(object? sender, RoutedEventArgs e)
    {
        ErrorTextBlock.Text = string.Empty;

        var username = UsernameTextBox.Text?.Trim() ?? string.Empty;
        var password = PasswordTextBox.Text ?? string.Empty;

        var user = App.dBcontext.Users.FirstOrDefault(u => u.Username == username);
        if (user is null || !AuthUtils.VerifyPassword(password, user.PasswordHash))
        {
            ErrorTextBlock.Text = "Неверный логин или пароль.";
            return;
        }

        App.CurrentUser = user;
        _mainWindow.ShowMainPage();
    }

    private void OnRegisterClick(object? sender, RoutedEventArgs e)
    {
        _mainWindow.ShowRegisterPage();
    }
}

