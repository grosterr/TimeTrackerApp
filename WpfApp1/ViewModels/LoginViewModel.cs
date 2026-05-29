using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using WpfApp1.Core;
using WpfApp1.Models;

namespace WpfApp1.ViewModels
{
    public class LoginViewModel : ObservableObject
    {
        private readonly ITaskRepository _repository;
        private readonly Action _onLoginSuccess;

        public string Username { get; set; }
        public string Password { get; set; }

        private bool _isRegisterMode;

        public bool IsRegisterMode
        {
            get => _isRegisterMode;
            set
            {
                _isRegisterMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ButtonText));
                OnPropertyChanged(nameof(LinkText));
            }
        }

        public string ButtonText => IsRegisterMode ? "ЗАРЕЄСТРУВАТИСЯ" : "УВІЙТИ";
        public string LinkText => IsRegisterMode ? "Вже є акаунт? Увійти" : "Зареєструватися";

        public RelayCommand LoginCommand { get; set; }
        public RelayCommand GuestCommand { get; set; }
        public RelayCommand ToggleModeCommand { get; set; }

        public LoginViewModel(ITaskRepository repository, Action onLoginSuccess)
        {
            _repository = repository;
            _onLoginSuccess = onLoginSuccess;

            LoginCommand = new RelayCommand(ExecuteLogin);
            GuestCommand = new RelayCommand(ExecuteGuest);
            ToggleModeCommand = new RelayCommand(o => IsRegisterMode = !IsRegisterMode);
        }

        private async void ExecuteLogin(object obj)
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                MessageBox.Show("Заповніть всі поля!");
                return;
            }

            var db = App.TaskRepository;

            if (IsRegisterMode)
            {
                var existing = await db.GetUserByUsernameAsync(Username);
                if (existing != null)
                {
                    MessageBox.Show("Користувач з таким логіном вже існує!");
                    return;
                }

                var user = new User
                {
                    Username = Username,
                    PasswordHash = PasswordHasher.HashPassword(Password)
                };

                try
                {
                    await db.RegisterUserAsync(user);
                    MessageBox.Show("Користувача створено! Тепер увійдіть.");
                    IsRegisterMode = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка реєстрації: {ex.Message}");
                }
            }
            else
            {
                var user = await db.GetUserByUsernameAsync(Username);
                if (user == null)
                {
                    MessageBox.Show("Користувача не знайдено!");
                    return;
                }

                var hash = PasswordHasher.HashPassword(Password);
                if (user.PasswordHash != hash)
                {
                    MessageBox.Show("Невірний пароль!");
                    return;
                }

                SessionManager.CurrentUserId = user.Id;
                SessionManager.CurrentUsername = user.Username;
                _onLoginSuccess?.Invoke();
            }
        }

        private void ExecuteGuest(object obj)
        {
            SessionManager.CurrentUserId = 0;
            SessionManager.CurrentUsername = "Гість";
            _onLoginSuccess?.Invoke();
        }
    }
}
