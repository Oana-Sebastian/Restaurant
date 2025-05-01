using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Restaurant.Models
{
    public class Category
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = "";
        public ICollection<Dish>? Dishes { get; set; }
        public ICollection<Menu>? Menus { get; set; }
    }
}
