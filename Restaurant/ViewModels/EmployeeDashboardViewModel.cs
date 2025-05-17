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
        public readonly INavigationService _nav;
        private readonly IServiceProvider _sp;
        private readonly IConfiguration _config;
        private readonly int _lowStockThreshold;
        private int _selectedTabIndex;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (_selectedTabIndex == value) return;
                _selectedTabIndex = value;
                OnPropertyChanged();

                
                if (_selectedTabIndex == 3)
                    RefreshLowStockItems();
            }
        }

        public ObservableCollection<Category> Categories { get; }
        public ObservableCollection<Dish> Dishes { get; }
        public ObservableCollection<MenuListItemViewModel> Menus { get; }
        public ObservableCollection<Allergen> Allergens { get; }

        public ObservableCollection<Order> AllOrders { get; }
        public ObservableCollection<Order> ActiveOrders { get; }
        public ObservableCollection<Dish> LowStockItems { get; }

        public Category? SelectedCategory
        {
            get;
            set;
        }
        public Dish? SelectedDish { get; set; }
        public MenuListItemViewModel? SelectedMenu { get; set; }
        public Allergen? SelectedAllergen { get; set; }
        public Order? SelectedActiveOrder { get; set; }

        public string NewCategoryName { get; set; } = "";
        public string NewAllergenName { get; set; } = "";

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
            _config = config;
            _lowStockThreshold = config.GetValue<int>("Settings:LowStockThreshold");


            Categories = new ObservableCollection<Category>(
                _db.Categories
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

           
            Menus = new ObservableCollection<MenuListItemViewModel>(
                _db.Menus
                    .Include(m => m.Category)
                    .Include(m => m.MenuItems)
                        .ThenInclude(mi => mi.Dish)
                            .ThenInclude(d => d.DishAllergens)
                                .ThenInclude(da => da.Allergen)
                    .Include(m => m.MenuItems)
                        .ThenInclude(mi => mi.Dish)
                            .ThenInclude(d => d.Images)
                    .AsNoTracking()
                    .ToList()
                    .Select(m => new MenuListItemViewModel(m, _config))
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
                      .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Menu)
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
                Dishes.Where(d => (d.TotalQuantity/d.PortionQuantity) <= _lowStockThreshold)
                      .ToList()
            );


            AddCategoryCommand = new RelayCommand(_ => AddCategory(), _ => !string.IsNullOrWhiteSpace(NewCategoryName));
            DeleteCategoryCommand = new RelayCommand(_ => DeleteCategory(SelectedCategory), _ => SelectedCategory != null && SelectedCategory.Name != string.Empty);

            AddAllergenCommand = new RelayCommand(_ => AddAllergen(), _ => !string.IsNullOrWhiteSpace(NewAllergenName));
            DeleteAllergenCommand = new RelayCommand(_ => DeleteAllergen(SelectedAllergen), _ => SelectedAllergen != null);

            AddDishCommand = new RelayCommand(_ =>
            {
                var dialog = _sp.GetRequiredService<AddEditDishWindow>();
                var vm = new DishViewModel(_db, _sp); 
                dialog.DataContext = vm;
                if (dialog.ShowDialog() == true)
                {
                    Dishes.Add(_db.Dishes
                                  .Include(d => d.Category)
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
                    var idx = Dishes.IndexOf(SelectedDish);
                    Dishes[idx] = _db.Dishes
                                       .Include(d => d.Category)
                                       .First(d => d.DishId == SelectedDish.DishId);

                }
            }, _ => SelectedDish != null);


            DeleteDishCommand = new RelayCommand(_ => DeleteDish(SelectedDish), _ => SelectedDish != null);

            AddMenuCommand = new RelayCommand(_ =>
            {
                var dlg = _sp.GetRequiredService<AddEditMenuWindow>();
                dlg.DataContext = new MenuViewModel(_db, _config);
                if (dlg.ShowDialog() == true)
                {
                    Menus.Clear();
                    

                    var raw = _db.Menus
                     .Include(x => x.Category)
                     .Include(x => x.MenuItems).ThenInclude(mi => mi.Dish)
                     .AsNoTracking()
                     .ToList();

                    foreach (var m in raw)
                    {
                        Menus.Add(new MenuListItemViewModel(m, _config));
                    }
                }
            });
            EditMenuCommand = new RelayCommand(_ =>
            {
                if (SelectedMenu == null) return;

                var dlg = _sp.GetRequiredService<AddEditMenuWindow>();
                dlg.DataContext = new MenuViewModel(_db, _config,SelectedMenu.Menu);

                if (dlg.ShowDialog() == true)
                {
                    
                    Menus.Clear();
                    foreach (var m in _db.Menus
                         .Include(x => x.Category)
                         .Include(x => x.MenuItems).ThenInclude(mi => mi.Dish)
                         .AsNoTracking()
                         .ToList())
                    {
                        Menus.Add(new MenuListItemViewModel(m, _config));
                    }
                }

            }, _ => SelectedMenu != null);
            DeleteMenuCommand = new RelayCommand(_ => DeleteMenu(SelectedMenu), _ => SelectedMenu != null);

            AdvanceOrderStatusCommand = new RelayCommand(_ => AdvanceOrderStatus(SelectedActiveOrder), _ => SelectedActiveOrder != null);

            LogoutCommand = new RelayCommand(_ =>
            {
                _auth.Logout();

                var dash = Application.Current.Windows
                             .OfType<EmployeeDashboardWindow>()
                             .FirstOrDefault();
                dash?.Close();

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

               var existing = _db.Categories
                      .Include(cat => cat.Dishes)
                      .Include(cat => cat.Menus)
                      .FirstOrDefault(cat => cat.CategoryId == c.CategoryId);

            if (existing == null)
            {
                MessageBox.Show("That category no longer exists.", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                Categories.Remove(c);
                return;
            }

            var usedByDishes = existing.Dishes?.Select(d => d.Name).ToList() ?? new();
            var usedByMenus = existing.Menus?.Select(m => m.Name).ToList() ?? new();

            if (usedByDishes.Any() || usedByMenus.Any())
            {
                var message = "This category is used by the following items:\n\n";
                if (usedByDishes.Any())
                    message += "Dishes:\n" + string.Join("\n", usedByDishes) + "\n\n";
                if (usedByMenus.Any())
                    message += "Menus:\n" + string.Join("\n", usedByMenus) + "\n\n";
                message += "Please remove or reassign these items before deleting the category.";

                MessageBox.Show(message, "Cannot delete category",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _db.Categories.Remove(existing);
            _db.SaveChanges();
            Categories.Remove(c);
        }


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

            var existing = _db.Allergens
                              .Include(al => al.DishAllergens)
                              .FirstOrDefault(al => al.AllergenId == a.AllergenId);

            if (existing == null)
            {
                MessageBox.Show("That allergen no longer exists.", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                Allergens.Remove(a);
                return;
            }

            var usedInDishes = existing.DishAllergens?
                            .Select(da => da.Dish.Name)
                            .Distinct()
                            .ToList() ?? new();

            if (usedInDishes.Any())
            {
                var message = "This allergen is associated with the following dishes:\n\n" +
                              string.Join("\n", usedInDishes) +
                              "\n\nPlease remove the allergen from these dishes before deleting it.";

                MessageBox.Show(message, "Cannot delete allergen",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _db.Allergens.Remove(existing);
            _db.SaveChanges();
            Allergens.Remove(a);
        }

        private void DeleteDish(Dish d)
        {
            if (d == null)
            {
                MessageBox.Show("No dish selected.", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var existing = _db.Dishes
                              .Include(dish => dish.DishAllergens)
                              .Include(dish => dish.Images)
                              .Include(dish => dish.MenuItems)
                              .ThenInclude(mi => mi.Menu)
                              .FirstOrDefault(x => x.DishId == d.DishId);

            if (existing == null)
            {
                MessageBox.Show("That dish no longer exists.", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                Dishes.Remove(d);
                return;
            }

            if (existing.MenuItems != null && existing.MenuItems.Any())
            {
                var menuNames = existing.MenuItems
                                        .Select(mi => mi.Menu?.Name)
                                        .Where(name => !string.IsNullOrWhiteSpace(name))
                                        .Distinct();

                string message = "This dish is part of the following menus:\n" +
                                 string.Join("\n", menuNames) +
                                 "\n\nPlease delete these menus first before deleting the dish.";

                MessageBox.Show(message, "Cannot Delete Dish",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _db.DishAllergens
   .RemoveRange(
      _db.DishAllergens
         .Where(da => da.DishId == existing.DishId)
   );
            _db.DishImages.RemoveRange(existing.Images);

            
            _db.Dishes.Remove(existing);
            _db.SaveChanges();

            Dishes.Remove(d);
        }

        private void DeleteMenu(MenuListItemViewModel vm)
        {
            if (vm == null)
            {
                MessageBox.Show("No menu selected.", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var menuId = vm.MenuId;
            var existing = _db.Menus.Find(vm.MenuId);
            if (existing == null)
            {
                MessageBox.Show("That menu no longer exists.", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                Menus.Remove(vm);
                return;
            }

            
            _db.MenuItems
              .RemoveRange(_db.MenuItems.Where(mi => mi.MenuId == menuId));
            _db.Menus.Remove(existing);
            _db.SaveChanges();

          
            Menus.Remove(vm);
        }

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

            
            existing.Status = existing.Status switch
            {
                OrderStatus.Registered => OrderStatus.Preparing,
                OrderStatus.Preparing => OrderStatus.OutForDelivery,
                OrderStatus.OutForDelivery => OrderStatus.Delivered,
                _ => existing.Status
            };
            _db.SaveChanges();

          
            if (existing.Status == OrderStatus.Delivered ||
    existing.Status == OrderStatus.Cancelled)

            {
                ActiveOrders.Remove(o);
            }
            RefreshActiveOrders();
        }

        private void RefreshActiveOrders()
        {
            ActiveOrders.Clear();
            var actives = _db.Orders
                .Where(o => o.Status != OrderStatus.Delivered
                         && o.Status != OrderStatus.Cancelled)
                .OrderBy(o => o.OrderDate)
                .Include(o => o.User)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Dish)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Menu);

            foreach (var ord in actives)
                ActiveOrders.Add(ord);
        }

        public void RefreshLowStockItems()
        {
            LowStockItems.Clear();
            foreach (var d in Dishes)
            {
                
                var portionsLeft = d.PortionQuantity > 0
                    ? d.TotalQuantity / d.PortionQuantity
                    : 0;

                if (portionsLeft <= _lowStockThreshold)
                    LowStockItems.Add(d);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string p = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }
}
