using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Restaurant.ViewModels;

namespace Restaurant.Views
{
    public partial class AddEditDishWindow : Window
    {
        public AddEditDishWindow()
        {
            InitializeComponent();
        }

        private void OnAddImageUrl(object sender, RoutedEventArgs e)
        {
            if (sender is Button && DataContext is DishViewModel vm)
            {
                var txt = (this.FindName("ImageUrlBox") as TextBox)?.Text.Trim();
                if (!string.IsNullOrEmpty(txt) && !vm.ImageUrls.Contains(txt))
                {
                    vm.ImageUrls.Add(txt);
                }
            }
        }

        private void AllergensListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is DishViewModel vm && sender is ListBox lb)
            {
                
                vm.SelectedAllergens.Clear();

                
                foreach (var item in lb.SelectedItems.Cast<Restaurant.Models.Allergen>())
                {
                    vm.SelectedAllergens.Add(item);
                }
            }
        }
    }
}
