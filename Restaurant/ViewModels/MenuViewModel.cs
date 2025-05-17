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
        private readonly RestaurantDbContext _db;
        private readonly IConfiguration _config;
        private readonly bool _isNew;
        private readonly int _discountPct;

        public ObservableCollection<CategoryGroupViewModel> Categories { get; }
        public ObservableCollection<MenuListItemViewModel> Menus { get; }
        public ObservableCollection<CategoryGroupViewModel> Groups { get; }
        private readonly IList<CategoryGroupViewModel> _allGroups;

        public string SearchQuery { get; set; }
        public ICommand SearchCommand { get; }

        public string Name { get; set; } = "";
        public ObservableCollection<Category> CatCategories { get; }
        private Category? _selectedCategory;
        public Category? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanSave));
                CommandManager.InvalidateRequerySuggested();
            }
        }


        public ObservableCollection<Dish> AvailableDishes { get; }
        private Dish? _selectedAvailableDish;
        public Dish? SelectedAvailableDish
        {
            get => _selectedAvailableDish;
            set { _selectedAvailableDish = value; OnPropertyChanged(); }
        }

        private int _newPortion;
        public int NewPortion
        {
            get => _newPortion;
            set { _newPortion = value; OnPropertyChanged(); }
        }

        public ObservableCollection<MenuItemDto> Items { get; }


        public ICommand AddItemCommand { get; }
        public ICommand RemoveItemCommand { get; }
        public DelegateCommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public bool CanSave =>
            !string.IsNullOrWhiteSpace(Name)
            && SelectedCategory != null
            && Items.Count >= 2;

        public int? MenuId { get; }  
        public MenuViewModel(RestaurantDbContext db, IConfiguration config, Menu? existing = null)
        {

            _db = db;
            _config = config;
            
            var rawCats = db.Categories
            .Include(c => c.Dishes)
                .ThenInclude(d => d.DishAllergens)
                    .ThenInclude(da => da.Allergen)
            .Include(c => c.Dishes)
                .ThenInclude(d => d.Images)
            .Include(c => c.Menus)
                .ThenInclude(m => m.MenuItems)
                    .ThenInclude(mi => mi.Dish)
                        .ThenInclude(d => d.DishAllergens)
                            .ThenInclude(da => da.Allergen)
            .Include(c => c.Menus)
                .ThenInclude(m => m.MenuItems)
                    .ThenInclude(mi => mi.Dish)
                        .ThenInclude(d => d.Images)
            .AsNoTracking()
            .ToList();

            _allGroups = rawCats
            .Select(c => new CategoryGroupViewModel(c, config))
            .ToList();
            Groups = new ObservableCollection<CategoryGroupViewModel>(_allGroups);

            SearchCommand = new RelayCommand(_ => ApplySearch());

            Categories = new ObservableCollection<CategoryGroupViewModel>(
            rawCats.Select(c => new CategoryGroupViewModel(c, config))
        );



            _discountPct = config.GetValue<int>("Settings:MenuDiscountPercent");
            CatCategories = new ObservableCollection<Category>(db.Categories.ToList());
            AvailableDishes = new ObservableCollection<Dish>(
                db.Dishes.AsNoTracking().ToList()
            );
            Items = new ObservableCollection<MenuItemDto>();
            Items.CollectionChanged += (_, __) =>
            {
                OnPropertyChanged(nameof(CanSave));
                CommandManager.InvalidateRequerySuggested();
            };


            if (existing != null)
            {
                _isNew = false;
                MenuId = existing.MenuId;
                Name = existing.Name;
                SelectedCategory = CatCategories.First(c => c.CategoryId == existing.CategoryId);

                foreach (var mi in existing.MenuItems)
                {
                    Items.Add(new MenuItemDto(
                        mi.DishId,
                        mi.Dish.Name,
                        mi.MenuPortionGrams
                    ));
                }
            }
            else
            {
                _isNew = true;
            }

            AddItemCommand = new RelayCommand(_ => AddItem(), _ => SelectedAvailableDish != null && NewPortion > 0);
            RemoveItemCommand = new RelayCommand(obj => RemoveItem(obj as MenuItemDto), obj => obj is MenuItemDto);
            SaveCommand = new DelegateCommand(win => Save(win as Window), _ => CanSave);
            Items.CollectionChanged += (_, __) =>
                SaveCommand.RaiseCanExecuteChanged();
            CancelCommand = new RelayCommand(win => { if (win is Window w) w.DialogResult = false; });

            
        }

        private void ApplySearch()
        {
            var results = SearchService.Filter(_allGroups, SearchQuery, minResults: 1);

            Groups.Clear();
            foreach (var g in results)
                Groups.Add(g);
        }

        private void AddItem()
        {
            if (SelectedAvailableDish == null) return;


            var existing = Items.FirstOrDefault(i => i.DishId == SelectedAvailableDish.DishId);
            if (existing != null)
            {
                existing.PortionGrams = NewPortion;
            }
            else
            {
                Items.Add(new MenuItemDto(
                    SelectedAvailableDish.DishId,
                    SelectedAvailableDish.Name,
                    NewPortion
                ));
            }
            NewPortion = 0;
        }

        private void RemoveItem(MenuItemDto? item)
        {
            if (item != null)
                Items.Remove(item);
        }

        private void Save(Window? dialog)
        {

            Menu menu;
            if (_isNew)
            {
                menu = new Menu
                {
                    Name = Name.Trim(),
                    CategoryId = SelectedCategory!.CategoryId
                };
                _db.Menus.Add(menu);
                _db.SaveChanges();
            }
            else
            {
                menu = _db.Menus.Find(MenuId!)!;
                menu.Name = Name.Trim();
                menu.CategoryId = SelectedCategory!.CategoryId;
                _db.Menus.Update(menu);
                _db.SaveChanges();


                _db.MenuItems.RemoveRange(
                    _db.MenuItems.Where(mi => mi.MenuId == menu.MenuId)
                );
                _db.SaveChanges();
            }


            foreach (var dto in Items)
            {
                _db.MenuItems.Add(new MenuItem
                {
                    MenuId = menu.MenuId,
                    DishId = dto.DishId,
                    MenuPortionGrams = dto.PortionGrams
                });
            }
            _db.SaveChanges();

            dialog!.DialogResult = true;
        }




        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string p = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));

    }
}