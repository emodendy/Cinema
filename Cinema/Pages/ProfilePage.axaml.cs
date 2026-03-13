using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Cinema.Authentication;
using Cinema.Data;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Pages;

public partial class ProfilePage : UserControl
{
    private readonly MainWindow _mainWindow;

    private List<Booking> _myBookings = new();

    public ProfilePage(MainWindow mainWindow)
    {
        InitializeComponent();
        _mainWindow = mainWindow;

        LoadUserInfo();
        LoadStats();
        LoadMyBookings();

        MyBookingsDataGrid.SelectionChanged += (_, _) => UpdateCancelButton();
    }

    private bool IsAdmin =>
        string.Equals(App.CurrentUser?.Role?.Name, "admin", StringComparison.OrdinalIgnoreCase);

    private void LoadUserInfo()
    {
        var user = App.CurrentUser;
        if (user is null)
        {
            UsernameTextBlock.Text = "-";
            RoleTextBlock.Text = "-";
            UsernameValueTextBlock.Text = "-";
            RoleValueTextBlock.Text = "-";
            return;
        }

        user.Role ??= App.dBcontext.Roles.FirstOrDefault(r => r.Id == user.RoleId);

        UsernameTextBlock.Text = user.Username;
        RoleTextBlock.Text = user.Role?.Name ?? "user";
        UsernameValueTextBlock.Text = user.Username;
        RoleValueTextBlock.Text = user.Role?.Name ?? "user";

        AdminOverviewTab.IsVisible = IsAdmin;
    }

    private void LoadStats()
    {
        if (!IsAdmin)
            return;

        var movies = App.dBcontext.Movies.Count();
        var halls = App.dBcontext.Halls.Count();
        var sessions = App.dBcontext.Sessions.Count();
        var seats = App.dBcontext.Seats.Count();
        var users = App.dBcontext.Users.Count();
        var bookings = App.dBcontext.Bookings.Count();

        StatsTextBlock.Text =
            $"Фильмы: {movies}\n" +
            $"Залы: {halls}\n" +
            $"Сеансы: {sessions}\n" +
            $"Места: {seats}\n" +
            $"Пользователи: {users}\n" +
            $"Бронирования: {bookings}";
    }

    private void LoadMyBookings()
    {
        var user = App.CurrentUser;
        if (user is null)
        {
            _myBookings = new List<Booking>();
            MyBookingsDataGrid.ItemsSource = _myBookings;
            CancelBookingButton.IsEnabled = false;
            return;
        }

        _myBookings = App.dBcontext.Bookings
            .Where(b => b.UserId == user.Id)
            .Include(b => b.Seat)
            .Include(b => b.Session)
            .ThenInclude(s => s!.Movie)
            .Include(b => b.Session)
            .ThenInclude(s => s!.Hall)
            .OrderByDescending(b => b.Id)
            .ToList();

        ApplyMyBookingsFilter();
    }

    private void ApplyMyBookingsFilter()
    {
        var query = _myBookings.AsEnumerable();

        var search = MyBookingsMovieSearchTextBox.Text?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(b => b.Session?.Movie != null &&
                                     b.Session.Movie.Title.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        MyBookingsDataGrid.ItemsSource = query.ToList();
        UpdateCancelButton();
    }

    private void UpdateCancelButton()
    {
        var user = App.CurrentUser;
        var booking = MyBookingsDataGrid.SelectedItem as Booking;
        CancelBookingButton.IsEnabled = user != null && booking != null && booking.UserId == user.Id;
    }

    private void OnMyBookingsFilterChanged(object? sender, TextChangedEventArgs e)
    {
        ApplyMyBookingsFilter();
    }

    private void OnCancelBookingClick(object? sender, RoutedEventArgs e)
    {
        var user = App.CurrentUser;
        if (user is null)
            return;

        if (MyBookingsDataGrid.SelectedItem is not Booking booking || booking.UserId != user.Id)
            return;

        App.dBcontext.Bookings.Remove(booking);
        App.dBcontext.SaveChanges();
        LoadMyBookings();
    }

    private void OnChangePasswordClick(object? sender, RoutedEventArgs e)
    {
        PasswordErrorTextBlock.Text = string.Empty;

        var user = App.CurrentUser;
        if (user is null)
            return;

        var oldPassword = OldPasswordTextBox.Text ?? string.Empty;
        var newPassword = NewPasswordTextBox.Text ?? string.Empty;
        var repeat = RepeatPasswordTextBox.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 4)
        {
            PasswordErrorTextBlock.Text = "Новый пароль слишком короткий.";
            return;
        }

        if (newPassword != repeat)
        {
            PasswordErrorTextBlock.Text = "Пароли не совпадают.";
            return;
        }

        var dbUser = App.dBcontext.Users.FirstOrDefault(u => u.Id == user.Id);
        if (dbUser is null)
        {
            PasswordErrorTextBlock.Text = "Пользователь не найден.";
            return;
        }

        if (!AuthUtils.VerifyPassword(oldPassword, dbUser.PasswordHash))
        {
            PasswordErrorTextBlock.Text = "Текущий пароль неверный.";
            return;
        }

        dbUser.PasswordHash = AuthUtils.HashPassword(newPassword);
        App.dBcontext.SaveChanges();

        OldPasswordTextBox.Text = string.Empty;
        NewPasswordTextBox.Text = string.Empty;
        RepeatPasswordTextBox.Text = string.Empty;
        PasswordErrorTextBlock.Text = "Пароль изменён.";
    }

    private void OnBackClick(object? sender, RoutedEventArgs e)
    {
        _mainWindow.ShowMainPage();
    }

    private void OnLogoutClick(object? sender, RoutedEventArgs e)
    {
        App.CurrentUser = null;
        _mainWindow.ShowLoginPage();
    }
}

