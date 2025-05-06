using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Restaurant.Models;

namespace Restaurant.ViewModels
{
    public class MenuListItemViewModel
    {
        public int MenuId { get; }
        public string Name { get; }
        public string CategoryName { get; }
        public decimal DiscountPct { get; }
        public decimal Price { get; }
        public bool IsAvailable { get; }
        public IEnumerable<string> Items { get; }

        public string AvailabilityText =>
            IsAvailable ? "" : "Indisponibil";

        public MenuListItemViewModel(Menu m, IConfiguration cfg)
        {
            MenuId = m.MenuId;
            Name = m.Name;
            CategoryName = m.Category.Name;
            DiscountPct = cfg.GetValue<decimal>("Settings:MenuDiscountPercent");

            // compute raw total
            var raw = m.MenuItems.Sum(mi =>
                mi.Dish.Price
                * mi.MenuPortionGrams
                / (decimal)mi.Dish.PortionQuantity
            );
            Price = Math.Round(raw * (1 - DiscountPct / 100M), 2);

            // availability
            IsAvailable = m.MenuItems.All(mi =>
                mi.Dish.TotalQuantity >= mi.MenuPortionGrams
            );

            // items list
            Items = m.MenuItems.Select(mi =>
                $"{mi.Dish.Name} – {mi.MenuPortionGrams}g"
            ).ToList();
        }
    }

}
