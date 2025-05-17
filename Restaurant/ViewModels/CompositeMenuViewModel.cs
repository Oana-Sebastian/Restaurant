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
            
            var discountPct = config.GetValue<decimal>("Settings:MenuDiscountPercent");

            
            Components = m.MenuItems
                          .Select(mi => (mi.Dish.Name,
                                          $"{mi.Quantity}g")) 
                          .ToArray();


            var sum = m.MenuItems.Sum(mi => mi.Dish.Price * mi.Quantity / mi.Dish.PortionQuantity);
            Price = Math.Round(sum * (1 - discountPct / 100M), 2);

            
            IsAvailable = m.MenuItems.All(mi =>
                mi.Dish.TotalQuantity >= mi.Quantity);
        }
    }
}
