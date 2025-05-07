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
using Microsoft.Extensions.Configuration;
using Restaurant.Models;

namespace Restaurant.ViewModels
{
    public class MenuListItemViewModel
    {
        public int MenuId { get; }
        public string Name { get; }
        public string CategoryName { get; }

        /// <summary>
        /// The per‐menu components: each dish + its menu‐specific gramaj.
        /// </summary>
        public IReadOnlyList<ComponentDto> Components { get; }

        /// <summary>
        /// Computed price after discount.
        /// </summary>
        public decimal Price { get; }

        /// <summary>
        /// Text shown when not available.
        /// </summary>
        public string AvailabilityText => IsAvailable ? "" : "Indisponibil";

        /// <summary>
        /// False means available, true means “show warning”.
        /// </summary>
        public bool IsAvailable { get; }

        /// <summary>
        /// All allergens across all component dishes, comma-separated.
        /// </summary>
        public string AllergensDisplay { get; }

        public MenuListItemViewModel(Menu m, IConfiguration cfg)
        {
            MenuId = m.MenuId;
            Name = m.Name;
            CategoryName = m.Category.Name;

            // Build components
            Components = m.MenuItems
                          .Select(mi => new ComponentDto(
                              mi.Dish.Name,
                              mi.MenuPortionGrams))
                          .ToList();

            // Price calculation
            var discountPct = cfg.GetValue<decimal>("Settings:MenuDiscountPercent") / 100M;
            var raw = m.MenuItems.Sum(mi =>
                mi.Dish.Price
                * mi.MenuPortionGrams
                / (decimal)mi.Dish.PortionQuantity);
            Price = Math.Round(raw * (1 - discountPct), 2);

            // Availability: all portions must be in stock
            IsAvailable = m.MenuItems.All(mi =>
                mi.Dish.TotalQuantity >= mi.MenuPortionGrams);

            // Allergens: distinct across all dishes
            var allergens = m.MenuItems
                             .SelectMany(mi => mi.Dish.DishAllergens)
                             .Select(da => da.Allergen.Name)
                             .Distinct()
                             .ToList();
            AllergensDisplay = allergens.Count > 0
                ? string.Join(", ", allergens)
                : "Fără alergeni";
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
