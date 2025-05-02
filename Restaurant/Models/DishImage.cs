using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Models
{
    public class DishImage
    {
        public int DishImageId { get; set; }
        public string Url { get; set; } = "";
        public int DishId { get; set; }
        public Dish Dish { get; set; } = null!;
    }
}
