using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Restaurant.Models;

namespace Restaurant.Models
{
    public enum OrderStatus { Registered, Preparing, OutForDelivery, Delivered, Cancelled }

    public class Order
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public DateTime OrderDate { get; set; }
        public OrderStatus Status { get; set; }
        public decimal TotalCost { get; set; }
        public TimeSpan EstimatedDeliveryTime { get; set; }
        public ICollection<OrderItem>? OrderItems { get; set; }
    }
}
