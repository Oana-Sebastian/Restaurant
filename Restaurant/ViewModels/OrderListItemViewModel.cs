using System;
using System.Linq;
using Restaurant.Models;
using Microsoft.Extensions.Configuration;

namespace Restaurant.ViewModels
{
    public class OrderListItemViewModel
    {
        public int OrderId { get; }
        public DateTime OrderDate { get; }
        public string Code { get; }
        public string ClientName { get; }
        public string ItemsDisplay { get; }
        public decimal FoodCost { get; }
        public decimal DeliveryFee { get; }
        public decimal DiscountAmount { get; }
        public decimal TotalCost { get; }
        public DateTime Eta { get; }
        public OrderStatus Status { get; }

        
        public OrderStatus? NextStatus { get; set; }

        public bool CanCancel => Status == OrderStatus.Registered;
        public bool CanChangeStatus => Status != OrderStatus.Delivered
                                     && Status != OrderStatus.Cancelled;

        public OrderListItemViewModel(Order o, IConfiguration cfg)
        {
            OrderId = o.OrderId;
            OrderDate = o.OrderDate;
            Code = o.Code;
            ClientName = $"{o.User.FirstName} {o.User.LastName}";
            FoodCost = o.FoodCost;
            DeliveryFee = o.DeliveryFee;
            DiscountAmount = o.DiscountAmount;
            TotalCost = o.FoodCost + o.DeliveryFee - o.DiscountAmount;
            Status = o.Status;

            Eta = o.OrderDate.AddMinutes(30);

            ItemsDisplay = string.Join(", ",
            o.OrderItems.Select(i =>
                i.Menu != null
                  ? $"{i.Quantity}×{i.Menu.Name} (meniu)"
                  : $"{i.Quantity}×{i.Dish!.Name}"
            )
        );
        }
    }
}
