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

      
        public ObservableCollection<Allergen> AvailableAllergens { get; }
        public ObservableCollection<Allergen> SelectedAllergens { get; }

        
        public ObservableCollection<string> ImageUrls { get; }

       
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

            
            Categories = new ObservableCollection<Category>(_db.Categories.ToList());
            AvailableAllergens = new ObservableCollection<Allergen>(_db.Allergens.ToList());

           


            var allergenSeq = _dish.DishAllergens?.Select(da => da.Allergen)
                  ?? Enumerable.Empty<Allergen>();

            SelectedAllergens = new ObservableCollection<Allergen>(
                allergenSeq.ToList()
            );

           

            var urlSeq = _dish.Images?.Select(i => i.Url)
             ?? Enumerable.Empty<string>();

            ImageUrls = new ObservableCollection<string>(
                urlSeq.ToList()
            );

            
            if (!_isNew)
                SelectedCategory = Categories.FirstOrDefault(c => c.CategoryId == _dish.CategoryId);

            
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
            
            if (_isNew)
            {
                _db.Dishes.Add(_dish);
                _db.SaveChanges();      
            }
            else
            {
                
                var tracked = _db.Dishes.Single(d => d.DishId == _dish.DishId);

                
                tracked.Name = _dish.Name;
                tracked.Price = _dish.Price;
                tracked.PortionQuantity = _dish.PortionQuantity;
                tracked.TotalQuantity = _dish.TotalQuantity;
                tracked.CategoryId = _dish.CategoryId;

                
                _db.SaveChanges();
            }


           
            var dishId = _dish.DishId;

            
            using (var scope = _sp.CreateScope())
            {
                var ctx = scope.ServiceProvider.GetRequiredService<RestaurantDbContext>();

                
                ctx.DishAllergens
                   .RemoveRange(ctx.DishAllergens.Where(da => da.DishId == dishId));

                
                ctx.DishImages
                   .RemoveRange(ctx.DishImages.Where(di => di.DishId == dishId));

                ctx.SaveChanges();

                
                foreach (var alg in SelectedAllergens.Distinct())
                {
                    ctx.DishAllergens.Add(new DishAllergen
                    {
                        DishId = dishId,
                        AllergenId = alg.AllergenId
                    });
                }

              
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
