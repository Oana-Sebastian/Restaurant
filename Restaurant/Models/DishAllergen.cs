using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Models
{
    public class DishAllergen
    {
        public int DishId { get; set; }
        public Dish? Dish { get; set; }
        public int AllergenId { get; set; }
        public Allergen? Allergen { get; set; }
    }
}

