//using System;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Microsoft.Extensions.Configuration;
//using Restaurant.Models;

//namespace Restaurant.ViewModels
//{
//    public class MenuListItemViewModel
//    {
//        public int MenuId { get; }
//        public string Name { get; }
//        public string CategoryName { get; }
//        public decimal DiscountPct { get; }
//        public decimal Price { get; }
//        public bool IsAvailable { get; }
//        public IEnumerable<string> Items { get; }
//        public ObservableCollection<MenuItem> MenuItems { get; set; }
//        public IEnumerable<string> AllergensList
//        {
//            get
//            {
//                return MenuItems?
//                       .SelectMany(mi => mi.Dish?.DishAllergens ?? Enumerable.Empty<DishAllergen>())
//                       .Select(da => da.Allergen?.Name)
//                       .Where(name => !string.IsNullOrWhiteSpace(name))
//                       .Distinct()
//                       .ToList();
//            }
//        }

//        public string AvailabilityText =>
//            IsAvailable ? "" : "Indisponibil";

//        public MenuListItemViewModel(Menu m, IConfiguration cfg)
//        {
//            MenuId = m.MenuId;
//            Name = m.Name;
//            CategoryName = m.Category.Name;
//            DiscountPct = cfg.GetValue<decimal>("Settings:MenuDiscountPercent");

//            // compute raw total
//            var raw = m.MenuItems.Sum(mi =>
//                mi.Dish.Price
//                * mi.MenuPortionGrams
//                / (decimal)mi.Dish.PortionQuantity
//            );
//            Price = Math.Round(raw * (1 - DiscountPct / 100M), 2);

//            // availability
//            IsAvailable = m.MenuItems.All(mi =>
//                mi.Dish.TotalQuantity >= mi.MenuPortionGrams
//            );

//            // items list
//            Items = m.MenuItems.Select(mi =>
//                $"{mi.Dish.Name} – {mi.MenuPortionGrams}g"
//            ).ToList();

//        }
//    }

//}



using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Restaurant.Data;
using Restaurant.Models;

namespace Restaurant.ViewModels
{
    public class MenuListItemViewModel
    {
        private readonly RestaurantDbContext _db;
        public int MenuId { get; }
        public string Name { get; }
        public string CategoryName { get; }
        public Menu Menu { get; set; }

       
        public IReadOnlyList<ComponentDto> Components { get; }

      
        public decimal Price { get; }

        
        public string AvailabilityText => IsAvailable ? "" : "Indisponibil";
        public bool IsAvailable { get; }

        
        public string MenuAllergensDisplay { get; }

        public MenuListItemViewModel(Menu m, IConfiguration cfg)
        {
            Menu = m;
            MenuId = m.MenuId;
            Name = m.Name;
            CategoryName = m.Category.Name;

           
            Components = m.MenuItems
                          .Select(mi => new ComponentDto(
                              mi.Dish.Name,
                              mi.MenuPortionGrams))
                          .ToList();

            
            var discountPct = cfg.GetValue<decimal>("Settings:MenuDiscountPercent") / 100M;
            var raw = m.MenuItems.Sum(mi =>
                mi.Dish.Price
                * mi.MenuPortionGrams
                / (decimal)mi.Dish.PortionQuantity);
            Price = Math.Round(raw * (1 - discountPct), 2);

            
            IsAvailable = m.MenuItems.All(mi =>
                mi.Dish.TotalQuantity >= mi.MenuPortionGrams);


            var allergens = m.MenuItems
                     .Where(mi => mi.Dish?.DishAllergens != null)
                     .SelectMany(mi => mi.Dish.DishAllergens)
                     .Where(da => da?.Allergen != null)
                     .Select(da => da.Allergen.Name)
                     .Distinct()
                     .ToList();
            MenuAllergensDisplay = allergens.Count > 0
                ? string.Join(", ", allergens)
                : "Fără alergeni";

            System.Diagnostics.Debug.WriteLine(
    $"Menu \"{Name}\" has {m.MenuItems.Count} items and " +
    $"{m.MenuItems.Sum(mi => mi.Dish.DishAllergens?.Count ?? 0)} allergen‐links"
);
        }

        public class ComponentDto
        {
            public string Dish { get; }
            public int Grams { get; }

            public ComponentDto(string dish, int grams)
            {
                Dish = dish;
                Grams = grams;
            }
        }
    }
}
