using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Restaurant.Helpers;    // RelayCommand
using Restaurant.Service;
using Restaurant.Views;    // IAuthService

namespace Restaurant.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged, INavigationService
    {
        private readonly IServiceProvider _sp;
        private readonly IAuthService _auth;
        private UserControl _currentView=null!;
         
        public UserControl CurrentView
        {
            get => _currentView;
            private set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        public ICommand NavigateLoginCommand { get; }
        public ICommand NavigateRegisterCommand { get; }
        public ICommand NavigateSearchCommand { get; }
        public ICommand NavigateMenuCommand { get; }
        public ICommand NavigateOrderCommand { get; }
        public ICommand NavigateManageCommand { get; }
        public ICommand LogoutCommand { get; }

        public MainWindowViewModel(IServiceProvider sp, IAuthService auth)
        {
            _sp = sp;
            _auth = auth;

            NavigateLoginCommand = new RelayCommand(_ => NavigateTo(nameof(LoginViewModel)),
                                                       _ => _auth.CurrentUser == null);
            NavigateRegisterCommand = new RelayCommand(_ => NavigateTo(nameof(RegisterViewModel)),
                                                       _ => _auth.CurrentUser == null);
          
            NavigateMenuCommand = new RelayCommand(_ => NavigateTo(nameof(MenuViewModel)),
                                                       _ => true);
            NavigateOrderCommand = new RelayCommand(_ => NavigateTo(nameof(OrderViewModel)),
                                                       _ => _auth.CurrentUser?.Role == Models.UserRole.Client);
            NavigateManageCommand = new RelayCommand(_ => NavigateTo(nameof(EmployeeDashboardViewModel)),
                                                       _ => _auth.CurrentUser?.Role == Models.UserRole.Employee);
            LogoutCommand = new RelayCommand(_ =>
            {
                _auth.Logout();
                CommandManager.InvalidateRequerySuggested();
                NavigateTo(nameof(LoginViewModel));
            },
                                                       _ => _auth.CurrentUser != null);
           
        }

        public void NavigateTo(string viewModelKey)
        {
            UserControl view;
            object viewModel;

            switch (viewModelKey)
            {
                case nameof(LoginViewModel):
                    view = _sp.GetRequiredService<LoginControl>();
                    viewModel = _sp.GetRequiredService<LoginViewModel>();
                    break;

                case nameof(RegisterViewModel):
                    view = _sp.GetRequiredService<RegisterControl>();
                    viewModel = _sp.GetRequiredService<RegisterViewModel>();
                    break;

                case nameof(EmployeeDashboardViewModel):
                    view = _sp.GetRequiredService<EmployeeDashboardControl>();
                    viewModel = _sp.GetRequiredService<EmployeeDashboardViewModel>();
                    break;
                case nameof(MenuViewModel):
                    view = _sp.GetRequiredService<MenuControl>();
                    viewModel = _sp.GetRequiredService<MenuViewModel>();
                    break;
                case nameof(OrderViewModel):
                    view = _sp.GetRequiredService<OrderControl>();
                    viewModel = _sp.GetRequiredService<OrderViewModel>();
                    break;
                

                default:
                    throw new InvalidOperationException($"Unknown view: {viewModelKey}");
            }

            view.DataContext = viewModel;

            CurrentView = view;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string prop = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
