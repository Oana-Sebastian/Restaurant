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
        public string Code { get; set; } = "";  
        public int UserId { get; set; }
        public User? User { get; set; }
        public DateTime OrderDate { get; set; }
        public OrderStatus Status { get; set; }

        public decimal FoodCost { get; set; }        
        public decimal DiscountAmount { get; set; }        
        public decimal DeliveryFee { get; set; }       

        [NotMapped]
        public decimal TotalCost { get; set; }        

        
        public TimeSpan EstimatedDeliveryTime { get; set; }

       
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
