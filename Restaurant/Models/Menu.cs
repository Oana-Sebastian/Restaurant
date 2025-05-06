using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Microsoft.Extensions.Configuration;

namespace Restaurant.Models
{
    public class Menu
    {
        public int MenuId { get; set; }
        public string Name { get; set; } = "";
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        public ICollection<MenuItem>? MenuItems { get; set; }

    }
}
