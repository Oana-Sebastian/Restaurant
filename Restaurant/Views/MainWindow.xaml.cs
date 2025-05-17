using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Restaurant.Service;
using System.Windows.Controls;
using Restaurant.ViewModels;
using Restaurant;


namespace Restaurant.Views
{
   
    public partial class MainWindow : Window
    {
        public MainWindow(MainWindowViewModel vm)
        {
            InitializeComponent();

            DataContext =vm;

            Loaded += (_, __) => vm.NavigateTo(nameof(LoginViewModel));
        }
    }
}
