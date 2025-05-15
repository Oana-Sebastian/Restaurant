using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Restaurant.Data;
using Restaurant.Helpers;
using Restaurant.Models;

namespace Restaurant.ViewModels
{
    public class DishViewModel : INotifyPropertyChanged
    {
        private readonly RestaurantDbContext _db;
        private readonly IServiceProvider _sp;
        private readonly Dish _dish;
        private readonly bool _isNew;
        public int DishId => _dish.DishId;
        // Form fields
        public string Name
        {
            get => _dish.Name;
            set { _dish.Name = value; OnPropertyChanged(); }
        }

        public decimal Price
        {
            get => _dish.Price;
            set { _dish.Price = value; OnPropertyChanged(); }
        }

        public int PortionQuantity
        {
            get => _dish.PortionQuantity;
            set { _dish.PortionQuantity = value; OnPropertyChanged(); }
        }

        public int TotalQuantity
        {
            get => _dish.TotalQuantity;
            set { _dish.TotalQuantity = value; OnPropertyChanged(); }
        }

        // Category selection
        public ObservableCollection<Category> Categories { get; }
        private Category? _selectedCategory;
        public Category? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                if (value != null) _dish.CategoryId = value.CategoryId;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSave));
            }
        }

        // Allergens multi‐select
        public ObservableCollection<Allergen> AvailableAllergens { get; }
        public ObservableCollection<Allergen> SelectedAllergens { get; }

        // Images
        public ObservableCollection<string> ImageUrls { get; }

        // Commands
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand DeleteImageUrlCommand { get; }

        public bool CanSave =>
            !string.IsNullOrWhiteSpace(Name)
            && Price > 0
            && PortionQuantity > 0
            && TotalQuantity >= 0
            && SelectedCategory != null;

        public DishViewModel(RestaurantDbContext db,
                             IServiceProvider sp,
                             Dish? dish = null)
        {
            _db = db;
            _sp = sp;
            _isNew = dish == null;
            _dish = dish ?? new Dish();

            // Load categories & allergens for the pick‐lists
            Categories = new ObservableCollection<Category>(_db.Categories.ToList());
            AvailableAllergens = new ObservableCollection<Allergen>(_db.Allergens.ToList());

            // Pre‐select existing allergens when editing
            //SelectedAllergens = new ObservableCollection<Allergen>(
            //    _dish.DishAllergens?
            //         .Select(da => da.Allergen)
            //         .ToList()
            //   ?? Array.Empty<Allergen>()
            //);


            var allergenSeq = _dish.DishAllergens?.Select(da => da.Allergen)
                  ?? Enumerable.Empty<Allergen>();

            SelectedAllergens = new ObservableCollection<Allergen>(
                allergenSeq.ToList()
            );

            // Images URLs
            //ImageUrls = new ObservableCollection<string>(
            //    _dish.Images?.Select(i => i.Url).ToList()
            //    ?? Array.Empty<string>()
            //);

            var urlSeq = _dish.Images?.Select(i => i.Url)
             ?? Enumerable.Empty<string>();

            ImageUrls = new ObservableCollection<string>(
                urlSeq.ToList()
            );

            // If editing, pick the category
            if (!_isNew)
                SelectedCategory = Categories.FirstOrDefault(c => c.CategoryId == _dish.CategoryId);

            // Commands
            SaveCommand = new RelayCommand(_ => Save(), _ => CanSave);
            CancelCommand = new RelayCommand(win =>
            {
                if (win is Window w) w.DialogResult = false;
            });

            DeleteImageUrlCommand = new RelayCommand(urlObj =>
            {
                if (urlObj is string url && ImageUrls.Contains(url))
                    ImageUrls.Remove(url);
            });
        }

        private void Save()
        {
            // 1) Save the Dish (and get its new DishId)
            if (_isNew)
            {
                _db.Dishes.Add(_dish);
                _db.SaveChanges();      // get a DishId
            }
            else
            {
                // Clear the Category navigation so EF only sees the FK

                // Mark the scalar props as modified
                //var entry = _db.Entry(_dish);
                //entry.Property(d => d.Name).IsModified = true;
                //entry.Property(d => d.Price).IsModified = true;
                //entry.Property(d => d.PortionQuantity).IsModified = true;
                //entry.Property(d => d.TotalQuantity).IsModified = true;
                //entry.Property(d => d.CategoryId).IsModified = true;

                //_db.SaveChanges();  // EF will update only those columns
                var tracked = _db.Dishes.Single(d => d.DishId == _dish.DishId);

                // 2) Apply your edits
                tracked.Name = _dish.Name;
                tracked.Price = _dish.Price;
                tracked.PortionQuantity = _dish.PortionQuantity;
                tracked.TotalQuantity = _dish.TotalQuantity;
                tracked.CategoryId = _dish.CategoryId;

                // 3) Save – EF will see the changes and issue an UPDATE
                _db.SaveChanges();
            }


            // Capture the new key
            var dishId = _dish.DishId;

            // 2) In a brand-new scoped context, delete & re-add join rows
            using (var scope = _sp.CreateScope())
            {
                var ctx = scope.ServiceProvider.GetRequiredService<RestaurantDbContext>();

                // Remove old allergen links
                ctx.DishAllergens
                   .RemoveRange(ctx.DishAllergens.Where(da => da.DishId == dishId));

                // Remove old images
                ctx.DishImages
                   .RemoveRange(ctx.DishImages.Where(di => di.DishId == dishId));

                ctx.SaveChanges();

                // Add new allergen links
                foreach (var alg in SelectedAllergens.Distinct())
                {
                    ctx.DishAllergens.Add(new DishAllergen
                    {
                        DishId = dishId,
                        AllergenId = alg.AllergenId
                    });
                }

                // Add new images
                foreach (var url in ImageUrls.Distinct())
                {
                    ctx.DishImages.Add(new DishImage
                    {
                        DishId = dishId,
                        Url = url
                    });
                }

                ctx.SaveChanges();
            }

            // 3) Close the dialog
            var win = Application.Current.Windows
                         .OfType<Window>()
                         .FirstOrDefault(w => w.DataContext == this);
            if (win != null) win.DialogResult = true;
        }



        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
