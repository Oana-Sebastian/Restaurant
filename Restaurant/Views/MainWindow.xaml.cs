using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Restaurant.Service;
using System.Windows.Controls;
using Restaurant.ViewModels;
using Restaurant;


namespace Restaurant.Views
{
    public partial class MainWindow : Window, INavigationService
    {
        private readonly IServiceProvider _sp;
         public MainWindow()
    {
        InitializeComponent();

        // Grab the container from App
        _sp = ((App)Application.Current).ServiceProvider;

            // Start on Login
           this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Now the window is fully built—safe to resolve sub-views
            NavigateTo(nameof(LoginViewModel));
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

                // … other views …
                default:
                    throw new InvalidOperationException($"Unknown view: {viewModelKey}");
            }

            // 1) Set the DataContext
            view.DataContext = viewModel;

            // 2) Swap into your ContentPresenter (or ContentControl)
            ContentArea.Content = view;
        }

    }
}
