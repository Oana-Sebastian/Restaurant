using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using Restaurant.Data;
using Restaurant.Helpers;
using Restaurant.Models;
using Restaurant.Service;

namespace Restaurant.ViewModels
{
    public class RegisterViewModel : INotifyPropertyChanged
    {
        private readonly RestaurantDbContext _db;
        private readonly INavigationService _nav;
        public RegisterViewModel(RestaurantDbContext db, INavigationService nav)
        {
            _db = db;
            _nav = nav;
            RegisterCommand = new RelayCommand(_ => ExecuteRegister(), _ => CanRegister);
        }

        private string _lastName = "";
        public string LastName
        {
            get => _lastName;
            set { _lastName = value; OnPropertyChanged();   OnPropertyChanged(nameof(CanRegister)); CommandManager.InvalidateRequerySuggested(); }
        }

        private string _firstName = "";
        public string FirstName
        {
            get => _firstName;
            set { _firstName = value; OnPropertyChanged();  OnPropertyChanged(nameof(CanRegister)); CommandManager.InvalidateRequerySuggested(); }
        }

        private string _email = "";
        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged();  OnPropertyChanged(nameof(CanRegister)); CommandManager.InvalidateRequerySuggested(); }
        }

        private string _phoneNumber = "";
        public string PhoneNumber
        {
            get => _phoneNumber;
            set { _phoneNumber = value; OnPropertyChanged();  OnPropertyChanged(nameof(CanRegister)); CommandManager.InvalidateRequerySuggested(); }
        }

        private string _deliveryAddress = "";
        public string DeliveryAddress
        {
            get => _deliveryAddress;
            set { _deliveryAddress = value; OnPropertyChanged();   OnPropertyChanged(nameof(CanRegister)); CommandManager.InvalidateRequerySuggested(); }
        }

        private string _password = "";
        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanRegister)); CommandManager.InvalidateRequerySuggested(); }
            }

        private string _confirmedPassword = "";
        public string ConfirmedPassword
        {
            get => _confirmedPassword;
            set { _confirmedPassword = value; OnPropertyChanged();   OnPropertyChanged(nameof(CanRegister)); CommandManager.InvalidateRequerySuggested(); }
        }


        public ICommand RegisterCommand { get; }
        public ICommand ShowLoginCommand => new RelayCommand(_ => ShowLogin());

        public bool CanRegister =>
            !string.IsNullOrWhiteSpace(LastName) &&
            !string.IsNullOrWhiteSpace(FirstName) &&
            !string.IsNullOrWhiteSpace(Email) &&
            !string.IsNullOrWhiteSpace(PhoneNumber) &&
            !string.IsNullOrWhiteSpace(DeliveryAddress) &&
            !string.IsNullOrWhiteSpace(Password) &&
            !string.IsNullOrWhiteSpace(ConfirmedPassword) &&
            Password == ConfirmedPassword;

        private void ExecuteRegister()
        {
            // 1) Basic empty checks already guaranteed by CanRegister

            // 2) Email format
            var emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            if (!Regex.IsMatch(Email.Trim(), emailPattern))
            {
                MessageBox.Show("Please enter a valid email address.", "Validation Error",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 3) Prevent duplicate emails
            if (_db.Users.Any(u => u.Email == Email.Trim()))
            {
                MessageBox.Show("An account with that email already exists.", "Validation Error",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 4) Phone number format: optional +, then 10–15 digits
            var phonePattern = @"^\+?\d{10,15}$";
            if (!Regex.IsMatch(PhoneNumber.Trim(), phonePattern))
            {
                MessageBox.Show("Please enter a valid phone number (10–15 digits, optional leading +).",
                                "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 5) Password strength: min 8 chars, at least one lowercase, one uppercase, one digit, one special
            var pwdPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,}$";
            if (!Regex.IsMatch(Password, pwdPattern))
            {
                MessageBox.Show(
                    "Password must be at least 8 characters long and include:\n" +
                    "- at least one uppercase letter\n" +
                    "- at least one lowercase letter\n" +
                    "- at least one digit\n" +
                    "- at least one special character",
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 6) Everything’s valid—determine role and create user
            var role = Email.Trim().EndsWith("@restaurant.com", StringComparison.OrdinalIgnoreCase)
                       ? UserRole.Employee
                       : UserRole.Client;

            var user = new User
            {
                FirstName = FirstName.Trim(),
                LastName = LastName.Trim(),
                Email = Email.Trim(),
                PhoneNumber = PhoneNumber.Trim(),
                Address = DeliveryAddress.Trim(),
                Role = role,
                PasswordHash = HashPassword(Password)
            };

            try
            {
                _db.Users.Add(user);
                _db.SaveChanges();

                MessageBox.Show(
                    role == UserRole.Employee
                        ? "Employee account created—please log in with your new credentials."
                        : "Client account created—please log in to continue.",
                    "Registration Successful",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                // Clear form
                FirstName = "";
                LastName = "";
                Email = "";
                PhoneNumber = "";
                DeliveryAddress = "";
                Password = "";
                ConfirmedPassword = "";

                // Navigate back to login
                _nav.NavigateTo(nameof(LoginViewModel));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to register: {ex.Message}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string HashPassword(string plain)
        {
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plain));
        }

        private void ShowLogin()
        {
            LastName = string.Empty;
            FirstName = string.Empty;
            Email = string.Empty;
            PhoneNumber = string.Empty;
            DeliveryAddress = string.Empty;
            Password = string.Empty;
            ConfirmedPassword = string.Empty;
            _nav.NavigateTo(nameof(LoginViewModel));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propName = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}
