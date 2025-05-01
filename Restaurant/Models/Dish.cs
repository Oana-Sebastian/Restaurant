using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Restaurant.Models
{
    public class Dish
    {
        public int DishId { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public int PortionQuantity { get; set; }    // e.g. 300 (grams)
        public int TotalQuantity { get; set; }    // in stock (grams)
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        public ICollection<DishAllergen>? DishAllergens { get; set; }
        public ICollection<MenuItem>? MenuItems { get; set; }
        public ICollection<OrderItem>? OrderItems { get; set; }
    }
}
