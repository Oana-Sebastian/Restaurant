using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Restaurant.Data;
using Restaurant.Helpers;
using Restaurant.Models;
using Restaurant.Service;
using Restaurant.Views;

namespace Restaurant.ViewModels
{
    public class EmployeeDashboardViewModel : INotifyPropertyChanged
    {
        private readonly RestaurantDbContext _db;
        private readonly IAuthService _auth;
        private readonly INavigationService _nav;
        private readonly IServiceProvider _sp;
        private readonly int _lowStockThreshold;

        public ObservableCollection<Category> Categories { get; }
        public ObservableCollection<Dish> Dishes { get; }
        public ObservableCollection<Menu> Menus { get; }
        public ObservableCollection<Allergen> Allergens { get; }

        public ObservableCollection<Order> AllOrders { get; }
        public ObservableCollection<Order> ActiveOrders { get; }
        public ObservableCollection<Dish> LowStockItems { get; }

        // Selected items
        public Category? SelectedCategory { 
            get; 
            set; }
        public Dish? SelectedDish { get; set; }
        public Menu? SelectedMenu { get; set; }
        public Allergen? SelectedAllergen { get; set; }
        public Order? SelectedActiveOrder { get; set; }

        // For new entries
        public string NewCategoryName { get; set; } = "";
        public string NewAllergenName { get; set; } = "";

        // Commands
        public ICommand AddCategoryCommand { get; }
        public ICommand DeleteCategoryCommand { get; }
        public ICommand AddDishCommand { get; }
        public ICommand EditDishCommand { get; }
        public ICommand DeleteDishCommand { get; }
        public ICommand AddMenuCommand { get; }
        public ICommand EditMenuCommand { get; }
        public ICommand DeleteMenuCommand { get; }
        public ICommand AddAllergenCommand { get; }
        public ICommand DeleteAllergenCommand { get; }
        public ICommand AdvanceOrderStatusCommand { get; }
        public ICommand LogoutCommand { get; }

        public EmployeeDashboardViewModel(
    RestaurantDbContext db,
    IConfiguration config,
    IAuthService auth,
    INavigationService nav,
    IServiceProvider sp)
        {
            _db = db;
            _auth = auth;
            _nav = nav;
            _sp = sp;
            _lowStockThreshold = config.GetValue<int>("Settings:LowStockThreshold");

            // LOAD ENTITIES WITH NAVIGATIONS

            Categories = new ObservableCollection<Category>(
                _db.Categories
                   // no navs needed here for delete-only UI
                   .AsNoTracking()
                   .ToList()
            );

            Dishes = new ObservableCollection<Dish>(
                _db.Dishes
                   .Include(d => d.Category)
                   .Include(d => d.DishAllergens)
                      .ThenInclude(da => da.Allergen)
                   .Include(d => d.Images)
                   .AsNoTracking()
                   .ToList()
            );

            Menus = new ObservableCollection<Menu>(
                _db.Menus
                   .Include(m => m.MenuItems)
                      .ThenInclude(mi => mi.Dish)
                        .ThenInclude(d => d.Category)
                   .Include(m => m.MenuItems)
                      .ThenInclude(mi => mi.Dish)
                        .ThenInclude(d => d.DishAllergens)
                          .ThenInclude(da => da.Allergen)
                   .Include(m => m.MenuItems)
                      .ThenInclude(mi => mi.Dish)
                        .ThenInclude(d => d.Images)
                   .AsNoTracking()
                   .ToList()
            );

            Allergens = new ObservableCollection<Allergen>(
                _db.Allergens
                   .AsNoTracking()
                   .ToList()
            );

            AllOrders = new ObservableCollection<Order>(
                _db.Orders
                   .Include(o => o.User)
                   .Include(o => o.OrderItems)
                      .ThenInclude(oi => oi.Dish)
                   .OrderByDescending(o => o.OrderDate)
                   .AsNoTracking()
                   .ToList()
            );

            ActiveOrders = new ObservableCollection<Order>(
                _db.Orders
                   .Include(o => o.User)
                   .Include(o => o.OrderItems)
                      .ThenInclude(oi => oi.Dish)
                   .Where(o => o.Status != OrderStatus.Delivered
                            && o.Status != OrderStatus.Cancelled)
                   .OrderByDescending(o => o.OrderDate)
                   .AsNoTracking()
                   .ToList()
            );

            LowStockItems = new ObservableCollection<Dish>(
                Dishes.Where(d => d.TotalQuantity <= _lowStockThreshold)
                      .ToList()
            );

            // COMMANDS

            AddCategoryCommand = new RelayCommand(_ => AddCategory(), _ => !string.IsNullOrWhiteSpace(NewCategoryName));
            DeleteCategoryCommand = new RelayCommand(_ =>DeleteCategory(SelectedCategory), _ => SelectedCategory != null && SelectedCategory.Name!=string.Empty);

            AddAllergenCommand = new RelayCommand(_ => AddAllergen(), _ => !string.IsNullOrWhiteSpace(NewAllergenName));
            DeleteAllergenCommand = new RelayCommand(_ => DeleteAllergen(SelectedAllergen), _ => SelectedAllergen != null);

            AddDishCommand = new RelayCommand(_ =>
            {
                var dialog = _sp.GetRequiredService<AddEditDishWindow>();
                var vm = new DishViewModel(_db, _sp); // new dish
                dialog.DataContext = vm;
                if (dialog.ShowDialog() == true)
                {
                    // Refresh the Dishes list:
                    Dishes.Add(_db.Dishes
                                  .Include(d => d.Category)
                                  // ... same includes as before ...
                                  .First(d => d.DishId == vm.DishId));
                }
            });

            EditDishCommand = new RelayCommand(_ =>
            {
                if (SelectedDish == null) return;
                var dialog = _sp.GetRequiredService<AddEditDishWindow>();
                var vm = new DishViewModel(_db, _sp, SelectedDish);
                dialog.DataContext = vm;
                if (dialog.ShowDialog() == true)
                {
                    // Re-load SelectedDish from DB or just notify:
                    var idx = Dishes.IndexOf(SelectedDish);
                    Dishes[idx] = _db.Dishes
                                       .Include(d => d.Category)
                                       // ... includes ...
                                       .First(d => d.DishId == SelectedDish.DishId);
                }
            }, _ => SelectedDish != null);
            DeleteDishCommand = new RelayCommand(_ => DeleteDish(SelectedDish), _ => SelectedDish != null);

            AddMenuCommand = new RelayCommand(_ => {/* Open AddMenuDialog bound to a new MenuViewModel */});
            EditMenuCommand = new RelayCommand(o => {/* Open EditMenuDialog bound to SelectedMenu */}, _ => SelectedMenu != null);
            DeleteMenuCommand = new RelayCommand(_ => DeleteMenu(SelectedMenu), _ => SelectedMenu != null);

            AdvanceOrderStatusCommand = new RelayCommand(o => AdvanceOrderStatus((Order)o), _ => SelectedActiveOrder != null);

            LogoutCommand = new RelayCommand(_ =>
            {
                // 1) Log out
                _auth.Logout();

                // 2) Close this dashboard window
                var dash = Application.Current.Windows
                             .OfType<EmployeeDashboardWindow>()
                             .FirstOrDefault();
                dash?.Close();

                // 3) Show main window and navigate back to login
                var main = Application.Current.Windows
                              .OfType<MainWindow>()
                              .FirstOrDefault();
                if (main != null)
                {
                    main.Show();
                    _nav.NavigateTo(nameof(LoginViewModel));
                }
            }, _ => _auth.CurrentUser != null);
        }


        // Category logic
// Categories
private void AddCategory()
{
    var name = NewCategoryName.Trim();
    if (string.IsNullOrEmpty(name))
    {
        MessageBox.Show("Category name cannot be empty.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
    }
    if (_db.Categories.Any(c => c.Name == name))
    {
        MessageBox.Show("A category with that name already exists.", "Duplicate Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
    }

    var cat = new Category { Name = name };
    _db.Categories.Add(cat);
    _db.SaveChanges();
    Categories.Add(cat);

    NewCategoryName = "";
    OnPropertyChanged(nameof(NewCategoryName));
}

private void DeleteCategory(Category c)
{
    if (c == null)
    {
        MessageBox.Show("No category selected.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
    }
    // re-fetch from DB to ensure it still exists
    var existing = _db.Categories.Find(c.CategoryId);
    if (existing == null)
    {
        MessageBox.Show("That category no longer exists.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
        Categories.Remove(c);
        return;
    }

    _db.Categories.Remove(existing);
    _db.SaveChanges();
    Categories.Remove(c);
}

// Allergens
private void AddAllergen()
{
    var name = NewAllergenName.Trim();
    if (string.IsNullOrEmpty(name))
    {
        MessageBox.Show("Allergen name cannot be empty.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
    }
    if (_db.Allergens.Any(a => a.Name == name))
    {
        MessageBox.Show("An allergen with that name already exists.", "Duplicate Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
    }

    var a = new Allergen { Name = name };
    _db.Allergens.Add(a);
    _db.SaveChanges();
    Allergens.Add(a);

    NewAllergenName = "";
    OnPropertyChanged(nameof(NewAllergenName));
}

private void DeleteAllergen(Allergen a)
{
    if (a == null)
    {
        MessageBox.Show("No allergen selected.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
    }
    var existing = _db.Allergens.Find(a.AllergenId);
    if (existing == null)
    {
        MessageBox.Show("That allergen no longer exists.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
        Allergens.Remove(a);
        return;
    }

    _db.Allergens.Remove(existing);
    _db.SaveChanges();
    Allergens.Remove(a);
}

// Dishes
private void DeleteDish(Dish d)
{
    if (d == null)
    {
        MessageBox.Show("No dish selected.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
    }
    var existing = _db.Dishes.Find(d.DishId);
    if (existing == null)
    {
        MessageBox.Show("That dish no longer exists.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
        Dishes.Remove(d);
        return;
    }

    _db.Dishes.Remove(existing);
    _db.SaveChanges();
    Dishes.Remove(d);
}

// Menus
private void DeleteMenu(Menu m)
{
    if (m == null)
    {
        MessageBox.Show("No menu selected.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
    }
    var existing = _db.Menus.Find(m.MenuId);
    if (existing == null)
    {
        MessageBox.Show("That menu no longer exists.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
        Menus.Remove(m);
        return;
    }

    _db.Menus.Remove(existing);
    _db.SaveChanges();
    Menus.Remove(m);
}

// Orders
private void AdvanceOrderStatus(Order o)
{
    if (o == null)
    {
        MessageBox.Show("No order selected.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
    }
    var existing = _db.Orders.Find(o.OrderId);
    if (existing == null)
    {
        MessageBox.Show("That order no longer exists.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
        ActiveOrders.Remove(o);
        AllOrders.Remove(o);
        return;
    }

    // advance enum
    existing.Status = existing.Status switch
    {
        OrderStatus.Registered     => OrderStatus.Preparing,
        OrderStatus.Preparing      => OrderStatus.OutForDelivery,
        OrderStatus.OutForDelivery => OrderStatus.Delivered,
        _                          => existing.Status
    };
    _db.SaveChanges();

    // refresh collections
    ActiveOrders.Remove(o);
    AllOrders.Clear();
    foreach (var ord in _db.Orders
                         .Include(x => x.User)
                         .OrderByDescending(x => x.OrderDate))
    {
        AllOrders.Add(ord);
    }
}


        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string p = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }
}
