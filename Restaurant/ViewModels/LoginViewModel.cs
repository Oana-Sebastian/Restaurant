using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Restaurant.Helpers;       
using Restaurant.Models;
using Restaurant.Service;        
using Restaurant.ViewModels;
using Restaurant.Views;     

namespace Restaurant.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly IAuthService _auth;
        private readonly INavigationService _nav;
        private readonly IServiceProvider _sp;

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
        public ICommand ShowMenuCommand { get; }

        public LoginViewModel(IAuthService auth, INavigationService nav, IServiceProvider sp)
        {
            _auth = auth;
            _nav = nav;
            _sp = sp;
            LoginCommand = new RelayCommand(_ => ExecuteLogin(), _ => CanLogin());
            ShowRegisterCommand = new RelayCommand(_ => ShowRegister());
            ShowMenuCommand = new RelayCommand(_ => ShowMenu());
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
            Email = string.Empty;
            Password = string.Empty;
            MessageBox.Show($"Welcome, {_auth.CurrentUser!.FirstName}!", "Login Successful",
                            MessageBoxButton.OK, MessageBoxImage.Information);

            if (_auth.CurrentUser!.Role == UserRole.Employee)
            {
                
                var main = Application.Current.Windows
                              .OfType<MainWindow>()
                              .FirstOrDefault();
                main?.Hide();
               
                var dash = _sp.GetRequiredService<EmployeeDashboardWindow>();
                dash.Show();
            }

         
            CommandManager.InvalidateRequerySuggested();

          
            if (_auth.CurrentUser.Role == Models.UserRole.Employee)
                _nav.NavigateTo(nameof(EmployeeDashboardViewModel));
            _nav.NavigateTo(nameof(MenuViewModel));
        }

        private void ShowMenu()
        {
            _nav.NavigateTo(nameof(MenuViewModel));
        }

        private void ShowRegister()
        {
            Email = string.Empty;
            Password = string.Empty;
            _nav.NavigateTo(nameof(RegisterViewModel));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propName = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}
