using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Restaurant.Helpers;        // RelayCommand
using Restaurant.Service;        // IAuthService
using Restaurant.ViewModels;     // for nameof(RegisterViewModel)

namespace Restaurant.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly IAuthService _auth;
        private readonly INavigationService _nav;

        private string _email = "";
        private string _password = "";

        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }

        public ICommand LoginCommand { get; }
        public ICommand ShowRegisterCommand { get; }

        public LoginViewModel(IAuthService auth, INavigationService nav)
        {
            _auth = auth;
            _nav = nav;

            LoginCommand = new RelayCommand(_ => ExecuteLogin(), _ => CanLogin());
            ShowRegisterCommand = new RelayCommand(_ => _nav.NavigateTo(nameof(RegisterViewModel)), _ => true);
        }

        private bool CanLogin() =>
            !string.IsNullOrWhiteSpace(Email) &&
            !string.IsNullOrWhiteSpace(Password);

        private void ExecuteLogin()
        {
            if (!_auth.Login(Email, Password))
            {
                MessageBox.Show("Invalid email or password.", "Login Failed",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show($"Welcome, {_auth.CurrentUser!.FirstName}!", "Login Successful",
                            MessageBoxButton.OK, MessageBoxImage.Information);

            // Refresh any role-based UI
            CommandManager.InvalidateRequerySuggested();

            // Go to the initial view for this role
            //if (_auth.CurrentUser.Role == Models.UserRole.Employee)
            //    _nav.NavigateTo(nameof(EmployeeDashboardViewModel));
            //else
            //    _nav.NavigateTo(nameof(MenuViewModel));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propName = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}
