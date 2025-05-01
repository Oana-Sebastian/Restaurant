using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Restaurant.Data;
using Restaurant.Helpers;
using Restaurant.Models;

namespace Restaurant.ViewModels
{
    public class RegisterViewModel : INotifyPropertyChanged
    {
        private readonly RestaurantDbContext _db;
        public RegisterViewModel(RestaurantDbContext db)
        {
            _db = db;
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
            // 1) Map ViewModel fields into a User entity
            var user = new User
            {
                FirstName = FirstName,
                LastName = LastName,
                Email = Email,
                PhoneNumber = PhoneNumber,
                Address = DeliveryAddress,
                Role = UserRole.Client,
                // TODO: hash the password rather than storing plain text:
                PasswordHash = HashPassword(Password)
            };

            try
            {
                // 2) Add and Save
                _db.Users.Add(user);
                _db.SaveChanges();

                MessageBox.Show("Registration successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // 3) Optionally navigate back to login:
                // _navigationService.NavigateTo(nameof(LoginViewModel));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to register: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string HashPassword(string plain)
        {
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plain));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propName = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}
