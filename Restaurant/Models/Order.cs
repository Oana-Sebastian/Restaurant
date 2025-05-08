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
        public string Code { get; set; } = "";   // unique order code
        public int UserId { get; set; }
        public User? User { get; set; }
        public DateTime OrderDate { get; set; }
        public OrderStatus Status { get; set; }

        // Breakdown of costs
        public decimal FoodCost { get; set; }        // sum of (price × quantity)
        public decimal DiscountAmount { get; set; }        // total discount applied
        public decimal DeliveryFee { get; set; }        // shipping fee if any

        // TotalCost can either be computed or stored:
        public decimal TotalCost { get; set; }        // FoodCost - DiscountAmount + DeliveryFee

        // The estimated delivery time (e.g. 30 minutes)
        public TimeSpan EstimatedDeliveryTime { get; set; }

        // Navigation to the line‐items
        public ICollection<OrderItem>? OrderItems { get; set; }
    }
}
