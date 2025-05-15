using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
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

        [NotMapped]
        public string ItemsDisplay
        {
            get
            {
                if (OrderItems == null || !OrderItems.Any())
                    return "";

                var parts = OrderItems.Select(i =>
                {
                    if (i.Menu != null)
                        return $"{i.Quantity}×{i.Menu.Name} (meniu)";
                    else if (i.Dish != null)
                        return $"{i.Quantity}×{i.Dish.Name}";
                    else
                        return $"{i.Quantity}×[Unknown item]";
                });

                return string.Join(", ", parts);
            }
        }

        [NotMapped]
        public string UserDisplay
        {
            get
            {
                if (User == null)
                    return "[Unknown user]";
                else
                    return $"{User.FirstName},{User.LastName},{User.PhoneNumber},{User.Address}";
            }
        }
    }
}
