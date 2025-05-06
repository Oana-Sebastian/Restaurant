using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Restaurant.Models;

namespace Restaurant.ViewModels
{
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
            var sum = m.MenuItems.Sum(mi => mi.Dish.Price * mi.Quantity / mi.Dish.PortionQuantity);
            Price = Math.Round(sum * (1 - discountPct / 100M), 2);

            // availability = all component dishes available
            IsAvailable = m.MenuItems.All(mi =>
                mi.Dish.TotalQuantity >= mi.Quantity);
        }
    }
}
