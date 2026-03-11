using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Cinema.Authentication;
using Cinema.Data;

namespace Cinema.Pages;

public partial class RegisterPage : UserControl
{
    private readonly MainWindow _mainWindow;

    public RegisterPage(MainWindow mainWindow)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
    }

    private void OnCreateClick(object? sender, RoutedEventArgs e)
    {
        ErrorTextBlock.Text = string.Empty;

        var username = UsernameTextBox.Text?.Trim() ?? string.Empty;
        var password = PasswordTextBox.Text ?? string.Empty;
        var asAdmin = IsAdminCheckBox.IsChecked == true;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ErrorTextBlock.Text = "Имя пользователя и пароль обязательны.";
            return;
        }

        if (App.dBcontext.Users.Any(u => u.Username == username))
        {
            ErrorTextBlock.Text = "Пользователь с таким именем уже существует.";
            return;
        }

        var roleName = asAdmin ? "admin" : "user";
        var role = App.dBcontext.Roles.FirstOrDefault(r => r.Name == roleName);
        if (role is null)
        {
            ErrorTextBlock.Text = "Роли ещё не инициализированы в базе данных.";
            return;
        }

        var user = new User
        {
            Username = username,
            PasswordHash = AuthUtils.HashPassword(password),
            RoleId = role.Id
        };

        App.dBcontext.Users.Add(user);
        App.dBcontext.SaveChanges();

        _mainWindow.ShowLoginPage();
    }

    private void OnBackClick(object? sender, RoutedEventArgs e)
    {
        _mainWindow.ShowLoginPage();
    }
}

