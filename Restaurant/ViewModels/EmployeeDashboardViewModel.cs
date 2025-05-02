using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        private readonly int _lowStockThreshold;

        public ObservableCollection<Category> Categories { get; }
        public ObservableCollection<Dish> Dishes { get; }
        public ObservableCollection<Menu> Menus { get; }
        public ObservableCollection<Allergen> Allergens { get; }

        public ObservableCollection<Order> AllOrders { get; }
        public ObservableCollection<Order> ActiveOrders { get; }
        public ObservableCollection<Dish> LowStockItems { get; }

        // Selected items
        public Category? SelectedCategory { get; set; }
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
    INavigationService nav)
        {
            _db = db;
            _auth = auth;
            _nav = nav;
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
            DeleteCategoryCommand = new RelayCommand(o => DeleteCategory((Category)o), _ => SelectedCategory != null);

            AddAllergenCommand = new RelayCommand(_ => AddAllergen(), _ => !string.IsNullOrWhiteSpace(NewAllergenName));
            DeleteAllergenCommand = new RelayCommand(o => DeleteAllergen((Allergen)o), _ => SelectedAllergen != null);

            AddDishCommand = new RelayCommand(_ => {/* Open AddDishDialog bound to a new DishViewModel */});
            EditDishCommand = new RelayCommand(o => {/* Open EditDishDialog bound to SelectedDish */}, _ => SelectedDish != null);
            DeleteDishCommand = new RelayCommand(o => DeleteDish((Dish)o), _ => SelectedDish != null);

            AddMenuCommand = new RelayCommand(_ => {/* Open AddMenuDialog bound to a new MenuViewModel */});
            EditMenuCommand = new RelayCommand(o => {/* Open EditMenuDialog bound to SelectedMenu */}, _ => SelectedMenu != null);
            DeleteMenuCommand = new RelayCommand(o => DeleteMenu((Menu)o), _ => SelectedMenu != null);

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
        private void AddCategory()
        {
            var cat = new Category { Name = NewCategoryName.Trim() };
            _db.Categories.Add(cat);
            _db.SaveChanges();
            Categories.Add(cat);
            NewCategoryName = "";
            OnPropertyChanged(nameof(NewCategoryName));
        }
        private void DeleteCategory(Category c)
        {
            _db.Categories.Remove(c);
            _db.SaveChanges();
            Categories.Remove(c);
        }

        // Allergen logic
        private void AddAllergen()
        {
            var a = new Allergen { Name = NewAllergenName.Trim() };
            _db.Allergens.Add(a);
            _db.SaveChanges();
            Allergens.Add(a);
            NewAllergenName = "";
            OnPropertyChanged(nameof(NewAllergenName));
        }
        private void DeleteAllergen(Allergen a)
        {
            _db.Allergens.Remove(a);
            _db.SaveChanges();
            Allergens.Remove(a);
        }

        // Dish/Menu logic & dialogs omitted for brevity…

        private void DeleteDish(Dish d)
        {
            _db.Dishes.Remove(d);
            _db.SaveChanges();
            Dishes.Remove(d);
        }
        private void DeleteMenu(Menu m)
        {
            _db.Menus.Remove(m);
            _db.SaveChanges();
            Menus.Remove(m);
        }

        // Orders
        private void AdvanceOrderStatus(Order o)
        {
            // advance enum, e.g. Registered→Preparing→…
            o.Status = o.Status switch
            {
                OrderStatus.Registered => OrderStatus.Preparing,
                OrderStatus.Preparing => OrderStatus.OutForDelivery,
                OrderStatus.OutForDelivery => OrderStatus.Delivered,
                _ => o.Status
            };
            _db.SaveChanges();

            // refresh collections
            ActiveOrders.Remove(o);
            AllOrders.Clear();
            foreach (var ord in _db.Orders.Include(x => x.User).OrderByDescending(x => x.OrderDate))
                AllOrders.Add(ord);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string p = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }
}
