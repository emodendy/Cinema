using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Controls.Templates;
using Cinema.Data;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Pages;

public partial class MainPage : UserControl
{
    private readonly MainWindow _mainWindow;

    private List<Movie> _allMovies = new();
    private List<Hall> _allHalls = new();
    private List<Session> _allSessions = new();
    private List<Seat> _allSeats = new();
    private List<Booking> _allBookings = new();

    public MainPage(MainWindow mainWindow)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
        LoadCurrentUserInfo();
        LoadAllData();
        ConfigurePermissions();
        BookingsDataGrid.SelectionChanged += (_, _) => UpdateCancelMyBookingButton();
    }

    private void LoadCurrentUserInfo()
    {
        var user = App.CurrentUser;
        if (user is null)
        {
            CurrentUserTextBlock.Text = "неизвестно";
            CurrentRoleTextBlock.Text = "-";
            return;
        }

        // Подтягиваем роль из БД на всякий случай
        user.Role ??= App.dBcontext.Roles.FirstOrDefault(r => r.Id == user.RoleId);

        CurrentUserTextBlock.Text = user.Username;
        CurrentRoleTextBlock.Text = user.Role?.Name ?? "user";
    }

    private bool IsAdmin =>
        string.Equals(App.CurrentUser?.Role?.Name, "admin", StringComparison.OrdinalIgnoreCase);

    private void ConfigurePermissions()
    {
        var isAdmin = IsAdmin;

        // Фильмы
        MoviesDataGrid.IsReadOnly = !isAdmin;
        AddMovieButton.IsEnabled = isAdmin;
        SaveMoviesButton.IsEnabled = isAdmin;
        DeleteMovieButton.IsEnabled = isAdmin;

        // Залы
        HallsDataGrid.IsReadOnly = !isAdmin;
        AddHallButton.IsEnabled = isAdmin;
        SaveHallsButton.IsEnabled = isAdmin;
        DeleteHallButton.IsEnabled = isAdmin;

        // Сеансы
        SessionsDataGrid.IsReadOnly = !isAdmin;
        AddSessionButton.IsEnabled = isAdmin;
        SaveSessionsButton.IsEnabled = isAdmin;
        DeleteSessionButton.IsEnabled = isAdmin;
        // Бронировать билет может любой залогиненный пользователь
        BookTicketButton.IsEnabled = App.CurrentUser is not null;

        // Места
        SeatsDataGrid.IsReadOnly = !isAdmin;
        AddSeatButton.IsEnabled = isAdmin;
        SaveSeatsButton.IsEnabled = isAdmin;
        DeleteSeatButton.IsEnabled = isAdmin;

        // Бронирования: админ удаляет любое, юзер — только свою
        DeleteBookingButton.IsEnabled = isAdmin;
        CancelMyBookingButton.IsEnabled = !isAdmin;
        UpdateCancelMyBookingButton();

        // Для обычного пользователя скрываем поиск по имени — видит только свои брони
        var showUserSearch = isAdmin;
        BookingUserSearchLabel.IsVisible = showUserSearch;
        BookingUserSearchTextBox.IsVisible = showUserSearch;
    }

    private void UpdateCancelMyBookingButton()
    {
        if (CancelMyBookingButton == null || !CancelMyBookingButton.IsEnabled) return;
        var user = App.CurrentUser;
        var booking = BookingsDataGrid.SelectedItem as Booking;
        CancelMyBookingButton.IsEnabled = user != null && booking != null && booking.UserId == user.Id;
    }

    private void LoadAllData()
    {
        LoadMovies();
        LoadHalls();
        LoadSessions();
        LoadSeats();
        LoadBookings();
    }

    // --- Фильмы ---
    private void LoadMovies()
    {
        _allMovies = App.dBcontext.Movies
            .OrderBy(m => m.Title)
            .ToList();
        ApplyMovieFilters();
    }

    private void ApplyMovieFilters()
    {
        var query = _allMovies.AsEnumerable();

        var search = MovieSearchTextBox.Text?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(m => m.Title.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (int.TryParse(MinDurationTextBox.Text, out var minDur))
        {
            query = query.Where(m => m.Duration >= minDur);
        }

        if (int.TryParse(MaxDurationTextBox.Text, out var maxDur))
        {
            query = query.Where(m => m.Duration <= maxDur);
        }

        MoviesDataGrid.ItemsSource = query.ToList();
    }

    private void OnMovieSearchChanged(object? sender, TextChangedEventArgs e) => ApplyMovieFilters();

    private void OnMovieFilterChanged(object? sender, TextChangedEventArgs e) => ApplyMovieFilters();

    private void OnResetMovieFiltersClick(object? sender, RoutedEventArgs e)
    {
        MovieSearchTextBox.Text = string.Empty;
        MinDurationTextBox.Text = string.Empty;
        MaxDurationTextBox.Text = string.Empty;
        ApplyMovieFilters();
    }

    private async void OnAddMovieClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new Window
        {
            Title = "Новый фильм",
            Width = 340,
            Height = 210
        };

        dialog.Content = BuildMovieDialogContent(dialog, out var titleBox, out var durationBox);

        var result = await dialog.ShowDialog<bool?>(_mainWindow);
        if (result == true)
        {
            if (string.IsNullOrWhiteSpace(titleBox.Text) || !int.TryParse(durationBox.Text, out var duration))
                return;

            var movie = new Movie
            {
                Title = titleBox.Text.Trim(),
                Duration = duration
            };

            App.dBcontext.Movies.Add(movie);
            App.dBcontext.SaveChanges();
            LoadMovies();
        }
    }

    private Control BuildMovieDialogContent(Window? dialog, out TextBox titleBox, out TextBox durationBox)
    {
        titleBox = new TextBox();
        durationBox = new TextBox();

        var okButton = new Button { Content = "ОК", IsDefault = true, Width = 90 };
        var cancelButton = new Button { Content = "Отмена", IsCancel = true, Width = 90 };

        okButton.Click += (_, _) => dialog?.Close(true);
        cancelButton.Click += (_, _) => dialog?.Close(false);

        return new StackPanel
        {
            Margin = new Avalonia.Thickness(16),
            Spacing = 8,
            Children =
            {
                new TextBlock { Text = "Название" },
                titleBox,
                new TextBlock { Text = "Длительность (мин)" },
                durationBox,
                new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Spacing = 8,
                    Children = { okButton, cancelButton }
                }
            }
        };
    }

    private void OnSaveMoviesClick(object? sender, RoutedEventArgs e)
    {
        App.dBcontext.SaveChanges();
        LoadMovies();
    }

    private void OnDeleteMovieClick(object? sender, RoutedEventArgs e)
    {
        if (MoviesDataGrid.SelectedItem is not Movie movie)
            return;

        App.dBcontext.Movies.Remove(movie);
        App.dBcontext.SaveChanges();
        LoadMovies();
    }

    // --- Залы ---
    private void LoadHalls()
    {
        _allHalls = App.dBcontext.Halls
            .OrderBy(h => h.Name)
            .ToList();
        ApplyHallFilters();
    }

    private void ApplyHallFilters()
    {
        var query = _allHalls.AsEnumerable();

        var search = HallSearchTextBox.Text?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(h => h.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        HallsDataGrid.ItemsSource = query.ToList();
    }

    private void OnHallSearchChanged(object? sender, TextChangedEventArgs e) => ApplyHallFilters();

    private async void OnAddHallClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new Window
        {
            Title = "Новый зал",
            Width = 320,
            Height = 180
        };

        var nameBox = new TextBox();
        var okButton = new Button { Content = "ОК", IsDefault = true, Width = 90 };
        var cancelButton = new Button { Content = "Отмена", IsCancel = true, Width = 90 };

        okButton.Click += (_, _) => dialog.Close(true);
        cancelButton.Click += (_, _) => dialog.Close(false);

        dialog.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(16),
            Spacing = 8,
            Children =
            {
                new TextBlock { Text = "Название зала" },
                nameBox,
                new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Spacing = 8,
                    Children = { okButton, cancelButton }
                }
            }
        };

        var result = await dialog.ShowDialog<bool?>(_mainWindow);
        if (result == true && !string.IsNullOrWhiteSpace(nameBox.Text))
        {
            var hall = new Hall { Name = nameBox.Text.Trim() };
            App.dBcontext.Halls.Add(hall);
            App.dBcontext.SaveChanges();
            LoadHalls();
        }
    }

    private void OnSaveHallsClick(object? sender, RoutedEventArgs e)
    {
        App.dBcontext.SaveChanges();
        LoadHalls();
    }

    private void OnDeleteHallClick(object? sender, RoutedEventArgs e)
    {
        if (HallsDataGrid.SelectedItem is not Hall hall)
            return;

        App.dBcontext.Halls.Remove(hall);
        App.dBcontext.SaveChanges();
        LoadHalls();
    }

    // --- Сеансы ---
    private void LoadSessions()
    {
        _allSessions = App.dBcontext.Sessions
            .Include(s => s.Movie)
            .Include(s => s.Hall)
            .OrderBy(s => s.StartTime)
            .ToList();

        ApplySessionFilters();
    }

    private void ApplySessionFilters()
    {
        var query = _allSessions.AsEnumerable();

        var movieSearch = SessionMovieSearchTextBox.Text?.Trim();
        if (!string.IsNullOrEmpty(movieSearch))
        {
            query = query.Where(s => s.Movie != null &&
                                     s.Movie.Title.Contains(movieSearch, StringComparison.OrdinalIgnoreCase));
        }

        if (SessionFromDatePicker.SelectedDate is { } fromOffsetFrom)
        {
            var fromDate = fromOffsetFrom.Date;
            query = query.Where(s => s.StartTime.Date >= fromDate);
        }

        if (SessionToDatePicker.SelectedDate is { } toOffsetTo)
        {
            var toDate = toOffsetTo.Date;
            query = query.Where(s => s.StartTime.Date <= toDate);
        }

        SessionsDataGrid.ItemsSource = query.ToList();
    }

    private void OnSessionFilterTextChanged(object? sender, TextChangedEventArgs e) => ApplySessionFilters();

    private void OnSessionDateFilterChanged(object? sender, DatePickerSelectedValueChangedEventArgs e) => ApplySessionFilters();

    private async void OnAddSessionClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new Window
        {
            Title = "Новый сеанс",
            Width = 420,
            Height = 320
        };

        var movies = App.dBcontext.Movies.OrderBy(m => m.Title).ToList();
        var halls = App.dBcontext.Halls.OrderBy(h => h.Name).ToList();

        var movieCombo = new ComboBox { ItemsSource = movies, SelectedIndex = movies.Count > 0 ? 0 : -1 };
        movieCombo.ItemTemplate = new FuncDataTemplate<Movie>((m, _) => new TextBlock { Text = m.Title }, true);

        var hallCombo = new ComboBox { ItemsSource = halls, SelectedIndex = halls.Count > 0 ? 0 : -1 };
        hallCombo.ItemTemplate = new FuncDataTemplate<Hall>((h, _) => new TextBlock { Text = h.Name }, true);
        var datePicker = new DatePicker { SelectedDate = DateTime.Today };
        var timeBox = new TextBox { Watermark = "ЧЧ:ММ, например 19:30" };
        var priceBox = new TextBox { Watermark = "Цена, например 350" };

        var okButton = new Button { Content = "ОК", IsDefault = true, Width = 90 };
        var cancelButton = new Button { Content = "Отмена", IsCancel = true, Width = 90 };

        okButton.Click += (_, _) => dialog.Close(true);
        cancelButton.Click += (_, _) => dialog.Close(false);

        dialog.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(16),
            Spacing = 8,
            Children =
            {
                new TextBlock { Text = "Фильм" },
                movieCombo,
                new TextBlock { Text = "Зал" },
                hallCombo,
                new TextBlock { Text = "Дата" },
                datePicker,
                new TextBlock { Text = "Время (локальное)" },
                timeBox,
                new TextBlock { Text = "Цена" },
                priceBox,
                new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Spacing = 8,
                    Children = { okButton, cancelButton }
                }
            }
        };

        var result = await dialog.ShowDialog<bool?>(_mainWindow);
        if (result == true)
        {
            if (movieCombo.SelectedItem is not Movie movie ||
                hallCombo.SelectedItem is not Hall hall ||
                datePicker.SelectedDate is null ||
                !TimeSpan.TryParse(timeBox.Text, out var time) ||
                !decimal.TryParse(priceBox.Text, out var price))
            {
                return;
            }

            var start = datePicker.SelectedDate.Value.Date + time;

            var session = new Session
            {
                MovieId = movie.Id,
                HallId = hall.Id,
                StartTime = start,
                Price = price
            };

            App.dBcontext.Sessions.Add(session);
            App.dBcontext.SaveChanges();
            LoadSessions();
        }
    }

    private void OnSaveSessionsClick(object? sender, RoutedEventArgs e)
    {
        App.dBcontext.SaveChanges();
        LoadSessions();
    }

    private void OnDeleteSessionClick(object? sender, RoutedEventArgs e)
    {
        if (SessionsDataGrid.SelectedItem is not Session session)
            return;

        App.dBcontext.Sessions.Remove(session);
        App.dBcontext.SaveChanges();
        LoadSessions();
    }

    // --- Места ---
    private void LoadSeats()
    {
        _allSeats = App.dBcontext.Seats
            .Include(s => s.Hall)
            .OrderBy(s => s.Hall!.Name)
            .ThenBy(s => s.Number)
            .ToList();

        ApplySeatFilters();
    }

    private void ApplySeatFilters()
    {
        var query = _allSeats.AsEnumerable();

        var hallSearch = SeatHallSearchTextBox.Text?.Trim();
        if (!string.IsNullOrEmpty(hallSearch))
        {
            query = query.Where(s => s.Hall != null &&
                                     s.Hall.Name.Contains(hallSearch, StringComparison.OrdinalIgnoreCase));
        }

        if (int.TryParse(SeatMinNumberTextBox.Text, out var minNumber))
        {
            query = query.Where(s => s.Number >= minNumber);
        }

        SeatsDataGrid.ItemsSource = query.ToList();
    }

    private void OnSeatFilterChanged(object? sender, TextChangedEventArgs e) => ApplySeatFilters();

    private async void OnAddSeatClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new Window
        {
            Title = "Новое место",
            Width = 360,
            Height = 220
        };

        var halls = App.dBcontext.Halls.OrderBy(h => h.Name).ToList();
        var hallCombo = new ComboBox { ItemsSource = halls, SelectedIndex = halls.Count > 0 ? 0 : -1 };
        hallCombo.ItemTemplate = new FuncDataTemplate<Hall>((h, _) => new TextBlock { Text = h.Name }, true);
        var numberBox = new TextBox { Watermark = "Номер места" };

        var okButton = new Button { Content = "ОК", IsDefault = true, Width = 90 };
        var cancelButton = new Button { Content = "Отмена", IsCancel = true, Width = 90 };

        okButton.Click += (_, _) => dialog.Close(true);
        cancelButton.Click += (_, _) => dialog.Close(false);

        dialog.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(16),
            Spacing = 8,
            Children =
            {
                new TextBlock { Text = "Зал" },
                hallCombo,
                new TextBlock { Text = "Номер места" },
                numberBox,
                new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Spacing = 8,
                    Children = { okButton, cancelButton }
                }
            }
        };

        var result = await dialog.ShowDialog<bool?>(_mainWindow);
        if (result == true)
        {
            if (hallCombo.SelectedItem is not Hall hall ||
                !int.TryParse(numberBox.Text, out var number))
            {
                return;
            }

            var seat = new Seat
            {
                HallId = hall.Id,
                Number = number
            };

            App.dBcontext.Seats.Add(seat);
            App.dBcontext.SaveChanges();
            LoadSeats();
        }
    }

    private void OnSaveSeatsClick(object? sender, RoutedEventArgs e)
    {
        App.dBcontext.SaveChanges();
        LoadSeats();
    }

    private void OnDeleteSeatClick(object? sender, RoutedEventArgs e)
    {
        if (SeatsDataGrid.SelectedItem is not Seat seat)
            return;

        App.dBcontext.Seats.Remove(seat);
        App.dBcontext.SaveChanges();
        LoadSeats();
    }

    // --- Бронирования (просмотр + удаление) ---
    private void LoadBookings()
    {
        _allBookings = App.dBcontext.Bookings
            .Include(b => b.User)
            .Include(b => b.Seat)
            .ThenInclude(s => s!.Hall)
            .Include(b => b.Session)
            .ThenInclude(s => s!.Movie)
            .Include(b => b.Session)
            .ThenInclude(s => s!.Hall)
            .ToList();

        ApplyBookingFilters();
    }

    private void ApplyBookingFilters()
    {
        var query = _allBookings.AsEnumerable();

        // Для обычного пользователя показываем только его бронирования
        var user = App.CurrentUser;
        if (!IsAdmin && user is not null)
        {
            query = query.Where(b => b.UserId == user.Id);
        }

        var userSearch = BookingUserSearchTextBox.Text?.Trim();
        if (!string.IsNullOrEmpty(userSearch))
        {
            query = query.Where(b => b.User != null &&
                                     b.User.Username.Contains(userSearch, StringComparison.OrdinalIgnoreCase));
        }

        var movieSearch = BookingMovieSearchTextBox.Text?.Trim();
        if (!string.IsNullOrEmpty(movieSearch))
        {
            query = query.Where(b => b.Session?.Movie != null &&
                                     b.Session.Movie.Title.Contains(movieSearch, StringComparison.OrdinalIgnoreCase));
        }

        BookingsDataGrid.ItemsSource = query.ToList();
    }

    private void OnBookingFilterChanged(object? sender, TextChangedEventArgs e) => ApplyBookingFilters();

    private void OnCancelMyBookingClick(object? sender, RoutedEventArgs e)
    {
        var user = App.CurrentUser;
        if (user is null) return;
        if (BookingsDataGrid.SelectedItem is not Booking booking || booking.UserId != user.Id) return;

        App.dBcontext.Bookings.Remove(booking);
        App.dBcontext.SaveChanges();
        LoadBookings();
        UpdateCancelMyBookingButton();
    }

    private void OnDeleteBookingClick(object? sender, RoutedEventArgs e)
    {
        if (BookingsDataGrid.SelectedItem is not Booking booking)
            return;

        App.dBcontext.Bookings.Remove(booking);
        App.dBcontext.SaveChanges();
        LoadBookings();
    }

    private async void OnBookTicketClick(object? sender, RoutedEventArgs e)
    {
        var user = App.CurrentUser;
        if (user is null) return;

        if (SessionsDataGrid.SelectedItem is not Session session)
        {
            await ShowMessageAsync("Выберите сеанс", "Сначала выберите сеанс в таблице.");
            return;
        }

        // Доступные места по залу сеанса
        var allSeatsForHall = App.dBcontext.Seats
            .Where(s => s.HallId == session.HallId)
            .OrderBy(s => s.Number)
            .ToList();

        var bookedSeatIds = App.dBcontext.Bookings
            .Where(b => b.SessionId == session.Id)
            .Select(b => b.SeatId)
            .ToHashSet();

        var availableSeats = allSeatsForHall
            .Where(s => !bookedSeatIds.Contains(s.Id))
            .ToList();

        if (availableSeats.Count == 0)
        {
            await ShowMessageAsync("Нет свободных мест", "На этот сеанс все места уже заняты.");
            return;
        }

        var dialog = new Window
        {
            Title = "Бронирование билета",
            Width = 460,
            Height = 320
        };

        var seatsCombo = new ComboBox
        {
            ItemsSource = availableSeats,
            SelectedIndex = 0,
            Width = 220
        };
        seatsCombo.ItemTemplate = new FuncDataTemplate<Seat>(
            (s, _) => new TextBlock { Text = $"Место {s.Number}" }, true);

        var selectedSeatText = new TextBlock { Classes = { "muted" } };
        void UpdateSelectedSeatText()
        {
            var idx = seatsCombo.SelectedIndex;
            if (idx >= 0 && idx < availableSeats.Count)
                selectedSeatText.Text = $"Выбрано: место {availableSeats[idx].Number}";
            else
                selectedSeatText.Text = "Выбрано: -";
        }
        seatsCombo.SelectionChanged += (_, _) => UpdateSelectedSeatText();
        UpdateSelectedSeatText();

        var okButton = new Button { Content = "Забронировать", IsDefault = true, Width = 140 };
        var cancelButton = new Button { Content = "Отмена", IsCancel = true, Width = 100, Classes = { "flat" } };

        okButton.Click += (_, _) => dialog.Close(true);
        cancelButton.Click += (_, _) => dialog.Close(false);

        dialog.Content = new Border
        {
            Padding = new Avalonia.Thickness(16),
            CornerRadius = new Avalonia.CornerRadius(10),
            BorderBrush = (Avalonia.Media.IBrush?)Avalonia.Application.Current?.Resources["App.BorderBrush"],
            BorderThickness = new Avalonia.Thickness(1),
            Background = (Avalonia.Media.IBrush?)Avalonia.Application.Current?.Resources["App.CardBackgroundBrush"],
            Child = new StackPanel
            {
                Spacing = 12,
                Children =
                {
                    new TextBlock { Text = session.Movie?.Title ?? "-", FontSize = 18, FontWeight = Avalonia.Media.FontWeight.Bold },
                    new TextBlock { Text = $"Зал: {session.Hall?.Name ?? "-"}", Classes = { "muted" } },
                    new TextBlock { Text = $"Начало: {session.StartTime:g}", Classes = { "muted" } },

                    new Border
                    {
                        Background = (Avalonia.Media.IBrush?)Avalonia.Application.Current?.Resources["App.BackgroundBrush"],
                        CornerRadius = new Avalonia.CornerRadius(8),
                        Padding = new Avalonia.Thickness(12),
                        Child = new StackPanel
                        {
                            Spacing = 8,
                            Children =
                            {
                                new TextBlock { Text = "Место", FontWeight = Avalonia.Media.FontWeight.SemiBold },
                                new StackPanel
                                {
                                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                                    Spacing = 12,
                                    Children = { seatsCombo, selectedSeatText }
                                }
                            }
                        }
                    },

                    new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                        Spacing = 8,
                        Children = { cancelButton, okButton }
                    }
                }
            }
        };

        var result = await dialog.ShowDialog<bool?>(_mainWindow);
        if (result == true)
        {
            var idx = seatsCombo.SelectedIndex;
            if (idx < 0 || idx >= availableSeats.Count)
                return;
            var seat = availableSeats[idx];

            var booking = new Booking
            {
                UserId = user.Id,
                SessionId = session.Id,
                SeatId = seat.Id
            };

            App.dBcontext.Bookings.Add(booking);
            App.dBcontext.SaveChanges();
            LoadBookings();
        }
    }

    private void OnLogoutClick(object? sender, RoutedEventArgs e)
    {
        App.CurrentUser = null;
        _mainWindow.ShowLoginPage();
    }

    private void OnProfileClick(object? sender, RoutedEventArgs e)
    {
        _mainWindow.ShowProfilePage();
    }

    private async System.Threading.Tasks.Task ShowMessageAsync(string title, string message)
    {
        var okButton = new Button { Content = "ОК", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };
        var dialog = new Window
        {
            Title = title,
            Width = 360,
            Height = 140,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(16),
                Spacing = 12,
                Children =
                {
                    new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                    okButton
                }
            }
        };
        okButton.Click += (_, _) => dialog.Close();
        await dialog.ShowDialog(_mainWindow);
    }
}

