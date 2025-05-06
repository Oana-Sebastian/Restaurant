using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Restaurant.Models;

namespace Restaurant.ViewModels
{
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
}
