using System;
using System.Collections.Generic;
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
        }

    }
}
