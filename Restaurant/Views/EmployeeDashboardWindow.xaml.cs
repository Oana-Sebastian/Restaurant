using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Restaurant.ViewModels;

namespace Restaurant.Views
{
    /// <summary>
    /// Interaction logic for EmployeeDashboardWindow.xaml
    /// </summary>
    public partial class EmployeeDashboardWindow : Window
    {
        public EmployeeDashboardWindow(EmployeeDashboardViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;

            this.Closed+= EmployeeDashboardWindow_Closed;
        
        }
        private void EmployeeDashboardWindow_Closed(object? sender, EventArgs e)
        {
            if (DataContext is EmployeeDashboardViewModel vm
             && vm.LogoutCommand.CanExecute(null))
            {
                // 1) Log out
                vm.LogoutCommand.Execute(null);

                // 2) Show main window & navigate back to login
                var main = Application.Current.Windows
                              .OfType<MainWindow>()
                              .FirstOrDefault();
                if (main != null)
                {
                    main.Show();
                    vm._nav.NavigateTo(nameof(LoginViewModel));
                }
            }
        }

    }

}
