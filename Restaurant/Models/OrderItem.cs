﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Restaurant.Models
{
    public class OrderItem
    {
        public int OrderItemId { get; set; }
        public int OrderId { get; set; }
        public Order? Order { get; set; }
        public int? DishId { get; set; }
        public Dish? Dish { get; set; }
        public int? MenuId { get; set; }
        public Menu? Menu { get; set; }
        public int Quantity { get; set; }
        public decimal PriceAtOrder { get; set; }
    }
}
