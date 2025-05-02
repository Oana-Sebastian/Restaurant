using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Restaurant.Data;
using Restaurant.Models;

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
                  // Eager‐load each dish’s allergens (via the join entity)
                  .Include(c => c.Dishes)
                    .ThenInclude(d => d.DishAllergens)
                      .ThenInclude(da => da.Allergen)

                  // Eager‐load each dish’s image gallery
                  .Include(c => c.Dishes)
                    .ThenInclude(d => d.Images)

                  // Eager‐load the menus in this category and their dish items
                  .Include(c => c.Menus)
                    .ThenInclude(m => m.MenuItems)
                      .ThenInclude(mi => mi.Dish)
                        // Also eager‐load those dishes’ allergens and images
                        .ThenInclude(d => d.DishAllergens)
                            .ThenInclude(da => da.Allergen)
                  // To get images on those menu component dishes:
                  .Include(c => c.Menus)
                    .ThenInclude(m => m.MenuItems)
                      .ThenInclude(mi => mi.Dish)
                        .ThenInclude(d => d.Images)

                  .AsNoTracking()

                  .Select(cat => new CategoryGroupViewModel(cat, config))
                  .ToList()
            );
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string p = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }

    public class CategoryGroupViewModel
    {
        public string Name { get; }
        public ObservableCollection<DishItemViewModel> Dishes { get; }
        public ObservableCollection<CompositeMenuViewModel> Menus { get; }

        public CategoryGroupViewModel(Category cat, IConfiguration config)
        {
            Name = cat.Name;

            // Dishes
            Dishes = new ObservableCollection<DishItemViewModel>(
                cat.Dishes.Select(d => new DishItemViewModel(d, config))
            );

            // Menus
            Menus = new ObservableCollection<CompositeMenuViewModel>(
                cat.Menus.Select(m => new CompositeMenuViewModel(m, config))
            );
        }
    }

    public class DishItemViewModel
    {
        public string Name { get; }
        public string PortionDisplay { get; }
        public decimal Price { get; }
        public string AllergensDisplay { get; }
        public string[] ImageUrls { get; }
        public bool IsAvailable { get; }
        public string AvailabilityText => IsAvailable ? "" : "Indisponibil";

        public DishItemViewModel(Dish d, IConfiguration config)
        {
            Name = d.Name;
            PortionDisplay = $"{d.PortionQuantity}g";            // your property
            Price = d.Price;
            AllergensDisplay = d.DishAllergens != null
            ? string.Join(", ",
                d.DishAllergens
                 .Select(da => da.Allergen?.Name ?? "")
                 .Where(n => !string.IsNullOrEmpty(n))
              )
            : string.Empty;
            ImageUrls = d.Images.Select(i => i.Url).ToArray();  // your nav
            // assume d.TotalQuantity holds total grams:
            IsAvailable = d.TotalQuantity >= d.PortionQuantity;
        }
    }

    public class CompositeMenuViewModel
    {
        public string Name { get; }
        public (string Dish, string Grams)[] Components { get; }
        public decimal Price { get; }
        public bool IsAvailable { get; }
        public string AvailabilityText => IsAvailable ? "" : "Indisponibil";

        public CompositeMenuViewModel(Menu m, IConfiguration config)
        {
            Name = m.Name;
            // read discount from config:
            var discountPct = config.GetValue<decimal>("Settings:MenuDiscountPercent");

            // Build list of dishes + their menu-specific portions
            Components = m.MenuItems
                          .Select(mi => (mi.Dish.Name,
                                          $"{mi.Quantity}g")) // assume property
                          .ToArray();

            // compute price
            var sum = m.MenuItems.Sum(mi => mi.Dish.Price * mi.Quantity/ mi.Dish.PortionQuantity);
            Price = Math.Round(sum * (1 - discountPct / 100M), 2);

            // availability = all component dishes available
            IsAvailable = m.MenuItems.All(mi =>
                mi.Dish.TotalQuantity >= mi.Quantity);
        }
    }
}
