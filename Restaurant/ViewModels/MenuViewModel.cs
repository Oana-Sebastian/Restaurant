using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Restaurant.Data;
using Restaurant.Models;
using Restaurant.Helpers;
using System.Windows.Input;
using System.Windows;

namespace Restaurant.ViewModels
{
    public class MenuViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<CategoryGroupViewModel> Categories { get; }

        public MenuViewModel(RestaurantDbContext db, IConfiguration config)
        {
            // Load all categories

            Categories = new ObservableCollection<CategoryGroupViewModel>(
        db.Categories
          .Include(c => c.Dishes)
            .ThenInclude(d => d.DishAllergens).ThenInclude(da => da.Allergen)
          .Include(c => c.Dishes).ThenInclude(d => d.Images)
          .Include(c => c.Menus).ThenInclude(m => m.MenuItems).ThenInclude(mi => mi.Dish)
          .AsNoTracking()
          .ToList()
          .Select(c => new CategoryGroupViewModel(c, config))
    );

            //Categories = new ObservableCollection<CategoryGroupViewModel>(
            //    db.Categories
            //      // Eager‐load each dish’s allergens (via the join entity)
            //      .Include(c => c.Dishes)
            //        .ThenInclude(d => d.DishAllergens)
            //          .ThenInclude(da => da.Allergen)

            //      // Eager‐load each dish’s image gallery
            //      .Include(c => c.Dishes)
            //        .ThenInclude(d => d.Images)

            //      // Eager‐load the menus in this category and their dish items
            //      .Include(c => c.Menus)
            //        .ThenInclude(m => m.MenuItems)
            //          .ThenInclude(mi => mi.Dish)
            //            // Also eager‐load those dishes’ allergens and images
            //            .ThenInclude(d => d.DishAllergens)
            //                .ThenInclude(da => da.Allergen)
            //      // To get images on those menu component dishes:
            //      .Include(c => c.Menus)
            //        .ThenInclude(m => m.MenuItems)
            //          .ThenInclude(mi => mi.Dish)
            //            .ThenInclude(d => d.Images)

            //      .AsNoTracking()

            //      .Select(cat => new CategoryGroupViewModel(cat, config))
            //      .ToList()
            //);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string p = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }

    //public class MenuViewModel : INotifyPropertyChanged
    //{
    //    private readonly RestaurantDbContext _db;
    //    private readonly bool _isNew;
    //    private readonly int _discountPct;

    //    // Form fields
    //    public string Name { get; set; } = "";
    //    public ObservableCollection<Category> Categories { get; }
    //    private Category? _selectedCategory;
    //    public Category? SelectedCategory
    //    {
    //        get => _selectedCategory;
    //        set
    //        {
    //            _selectedCategory = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanSave));
    //            CommandManager.InvalidateRequerySuggested();
    //        }
    //    }

    //    // Dishes-to-add picklist
    //    public ObservableCollection<Dish> AvailableDishes { get; }
    //    private Dish? _selectedAvailableDish;
    //    public Dish? SelectedAvailableDish
    //    {
    //        get => _selectedAvailableDish;
    //        set { _selectedAvailableDish = value; OnPropertyChanged(); }
    //    }

    //    private int _newPortion;
    //    public int NewPortion
    //    {
    //        get => _newPortion;
    //        set { _newPortion = value; OnPropertyChanged(); }
    //    }

    //    // Current menu items
    //    public ObservableCollection<MenuItemDto> Items { get; }

    //    // Commands
    //    public ICommand AddItemCommand { get; }
    //    public ICommand RemoveItemCommand { get; }
    //    public DelegateCommand SaveCommand { get; }
    //    public ICommand CancelCommand { get; }

    //    public bool CanSave =>
    //        !string.IsNullOrWhiteSpace(Name)
    //        && SelectedCategory != null
    //        && Items.Count >= 2;

    //    public int? MenuId { get; }  // for edit

    //    public MenuViewModel(
    //        RestaurantDbContext db,
    //        IConfiguration cfg,
    //        Menu? existing = null)
    //    {
    //        _db = db;
    //        _discountPct = cfg.GetValue<int>("Settings:MenuDiscountPercent");
    //        Categories = new ObservableCollection<Category>(db.Categories.ToList());
    //        AvailableDishes = new ObservableCollection<Dish>(
    //            db.Dishes.AsNoTracking().ToList()
    //        );
    //        Items = new ObservableCollection<MenuItemDto>();
    //        Items.CollectionChanged += (_, __) =>
    //        {
    //            OnPropertyChanged(nameof(CanSave));
    //            CommandManager.InvalidateRequerySuggested();
    //        };


    //        if (existing != null)
    //        {
    //            _isNew = false;
    //            MenuId = existing.MenuId;
    //            Name = existing.Name;
    //            SelectedCategory = Categories.First(c => c.CategoryId == existing.CategoryId);

    //            foreach (var mi in existing.MenuItems)
    //            {
    //                Items.Add(new MenuItemDto(
    //                    mi.DishId,
    //                    mi.Dish.Name,
    //                    mi.MenuPortionGrams
    //                ));
    //            }
    //        }
    //        else
    //        {
    //            _isNew = true;
    //        }

    //        AddItemCommand = new RelayCommand(_ => AddItem(), _ => SelectedAvailableDish != null && NewPortion > 0);
    //        RemoveItemCommand = new RelayCommand(obj => RemoveItem(obj as MenuItemDto), obj => obj is MenuItemDto);
    //        SaveCommand = new DelegateCommand(win => Save(win as Window), _ => CanSave);
    //        Items.CollectionChanged += (_, __) =>
    //            SaveCommand.RaiseCanExecuteChanged();
    //        CancelCommand = new RelayCommand(win => { if (win is Window w) w.DialogResult = false; });
    //    }

    //    private void AddItem()
    //    {
    //        if (SelectedAvailableDish == null) return;

    //        // prevent duplicates: replace if exists
    //        var existing = Items.FirstOrDefault(i => i.DishId == SelectedAvailableDish.DishId);
    //        if (existing != null)
    //        {
    //            existing.PortionGrams = NewPortion;
    //        }
    //        else
    //        {
    //            Items.Add(new MenuItemDto(
    //                SelectedAvailableDish.DishId,
    //                SelectedAvailableDish.Name,
    //                NewPortion
    //            ));
    //        }
    //        NewPortion = 0;
    //    }

    //    private void RemoveItem(MenuItemDto? item)
    //    {
    //        if (item != null)
    //            Items.Remove(item);
    //    }

    //    private void Save(Window? dialog)
    //    {
    //        // 1) Save the Menu entity
    //        Menu menu;
    //        if (_isNew)
    //        {
    //            menu = new Menu
    //            {
    //                Name = Name.Trim(),
    //                CategoryId = SelectedCategory!.CategoryId
    //            };
    //            _db.Menus.Add(menu);
    //            _db.SaveChanges();
    //        }
    //        else
    //        {
    //            menu = _db.Menus.Find(MenuId!)!;
    //            menu.Name = Name.Trim();
    //            menu.CategoryId = SelectedCategory!.CategoryId;
    //            _db.Menus.Update(menu);
    //            _db.SaveChanges();

    //            // Clear old MenuItems
    //            _db.MenuItems.RemoveRange(
    //                _db.MenuItems.Where(mi => mi.MenuId == menu.MenuId)
    //            );
    //            _db.SaveChanges();
    //        }

    //        // 2) Insert new MenuItems
    //        foreach (var dto in Items)
    //        {
    //            _db.MenuItems.Add(new MenuItem
    //            {
    //                MenuId = menu.MenuId,
    //                DishId = dto.DishId,
    //                MenuPortionGrams = dto.PortionGrams
    //            });
    //        }
    //        _db.SaveChanges();

    //        dialog!.DialogResult = true;
    //    }

    //    public event PropertyChangedEventHandler? PropertyChanged;
    //    private void OnPropertyChanged([CallerMemberName] string p = "") =>
    //        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    //}



}
