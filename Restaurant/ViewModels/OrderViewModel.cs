using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Restaurant.Data;
using Restaurant.Helpers;
using Restaurant.Models;
using Restaurant.Service;

namespace Restaurant.ViewModels
{
    public class OrderViewModel : INotifyPropertyChanged
    {
        private readonly RestaurantDbContext _db;
        private readonly IAuthService _auth;
        private readonly IConfiguration _config;

       
        private readonly decimal _freeDeliveryThreshold;
        private readonly decimal _deliveryFee;
        private readonly decimal _orderDiscountThreshold;
        private readonly int _bulkOrderCount;
        private readonly TimeSpan _bulkOrderWindow;
        private readonly decimal _orderDiscount;

        public ObservableCollection<OrderableItem> MenuItemsForOrdering { get; }
        public OrderableItem SelectedOrderItem { get; set; }
        public string NewOrderQuantity { get; set; } = "1";
        public ICommand AddToCartCommand { get; }
        public ICommand PlaceOrderCommand { get; }
        public ICommand RemoveFromCartCommand { get; }
        public ICommand CancelOrderCommand { get; }
        public ICommand ChangeStatusCommand { get; }

       
        public ObservableCollection<CartItemViewModel> CartItems { get; }
    = new ObservableCollection<CartItemViewModel>();

        public ObservableCollection<OrderListItemViewModel> DisplayedOrders { get; }
            = new ObservableCollection<OrderListItemViewModel>();

        public OrderListItemViewModel SelectedOrder { get; set; }

        public bool CanAddToCart =>
            SelectedOrderItem != null
         && int.TryParse(NewOrderQuantity, out var q) && q > 0;

        public bool CanPlaceOrder => CartItems.Any();

        public bool CanCancel(OrderListItemViewModel vm) =>
            vm.Status == OrderStatus.Registered;

        public bool CanChangeStatus => _auth.CurrentUser?.Role == UserRole.Employee;

        public ObservableCollection<OrderStatus> AvailableStatuses { get; }
            = new ObservableCollection<OrderStatus>(
                Enum.GetValues(typeof(OrderStatus)).Cast<OrderStatus>()
              );


        public OrderViewModel(RestaurantDbContext db,
                              IAuthService auth,
                              IConfiguration config)
        {
            _db = db; _auth = auth; _config = config;

            var discountPct = _config.GetValue<int>("Settings:MenuDiscountPercent");

            
            var cats = db.Categories
              
                .Include(c => c.Dishes)
                    .ThenInclude(d => d.DishAllergens)   
                        .ThenInclude(da => da.Allergen)
                .Include(c => c.Dishes)
                    .ThenInclude(d => d.Images)
                .Include(c => c.Menus)
                    .ThenInclude(m => m.MenuItems)
                        .ThenInclude(mi => mi.Dish)
                            .ThenInclude(d => d.DishAllergens)
                                .ThenInclude(da => da.Allergen)
                .Include(c => c.Menus)
                    .ThenInclude(m => m.MenuItems)
                        .ThenInclude(mi => mi.Dish)
                            .ThenInclude(d => d.Images)
                .AsNoTracking()
                .ToList();

            _freeDeliveryThreshold = config.GetValue<decimal>("Settings:FreeDeliveryThreshold");
            _deliveryFee = config.GetValue<decimal>("Settings:DeliveryFee");
            _orderDiscountThreshold = config.GetValue<decimal>("Settings:OrderDiscountThreshold");
            _bulkOrderCount = config.GetValue<int>("Settings:BulkOrderCountThreshold");
            _bulkOrderWindow = TimeSpan.FromHours(
                                         config.GetValue<int>("Settings:BulkOrderTimeWindowHours"));
            _orderDiscount = config.GetValue<decimal>("Settings:OrderDiscountPercent");

           
            var orderables = new List<OrderableItem>();
            foreach (var c in cats)
            {
                foreach (var d in c.Dishes)
                    orderables.Add(new OrderableItem(d));
                foreach (var m in c.Menus)
                    orderables.Add(new OrderableItem(m, discountPct));
            }

            MenuItemsForOrdering = new ObservableCollection<OrderableItem>(orderables);

            AddToCartCommand = new RelayCommand(_ => AddToCart(), _ => CanAddToCart);
            PlaceOrderCommand = new RelayCommand(_ => PlaceOrder(), _ => CanPlaceOrder);

            CancelOrderCommand = new RelayCommand(o => Cancel((OrderListItemViewModel)o));
            ChangeStatusCommand = new RelayCommand(o => ChangeStatus((OrderListItemViewModel)o));
            RemoveFromCartCommand = new RelayCommand(
        ci => RemoveCartItem((CartItemViewModel)ci),
        ci => ci is CartItemViewModel
    );
            RefreshDisplayedOrders();
        }

            private void AddToCart()
        {
            if (SelectedOrderItem == null) return;
            if (!int.TryParse(NewOrderQuantity, out var q) || q <= 0) return;

            
            var shortfalls = new List<string>();
            if (!SelectedOrderItem.IsMenu)
            {
                var dish = (Dish)SelectedOrderItem.Entity;
                var needed = q * dish.PortionQuantity;
                if (dish.TotalQuantity < needed)
                    shortfalls.Add($"{dish.Name}: stoc {dish.TotalQuantity / dish.PortionQuantity}, ai cerut {q}");
            }
            else
            {
                var menu = (Menu)SelectedOrderItem.Entity;
                foreach (var mi in menu.MenuItems)
                {
                    var portionsNeeded = (int)Math.Ceiling(q * mi.MenuPortionGrams / (double)mi.Dish.PortionQuantity);
                    if (mi.Dish.TotalQuantity < portionsNeeded * mi.Dish.PortionQuantity)
                        shortfalls.Add(
                          $"{mi.Dish.Name}: stoc {mi.Dish.TotalQuantity / mi.Dish.PortionQuantity}, " +
                          $"ai cerut {portionsNeeded}"
                        );
                }
            }

            if (shortfalls.Any())
            {
                MessageBox.Show(
                    "Nu se poate adăuga în coș din cauza stocului insuficient:\n\n" +
                    string.Join("\n", shortfalls),
                    "Stoc insuficient",
                    MessageBoxButton.OK, MessageBoxImage.Warning
                );
                return;
            }

            var existing = CartItems.FirstOrDefault(ci => ci.DisplayText == SelectedOrderItem.DisplayText);
            if (existing != null)
            {
                existing.Increase(q);
            }
            else
            {
                CartItems.Add(new CartItemViewModel(SelectedOrderItem, q));
            }

            NewOrderQuantity = "1";
            OnPropertyChanged(nameof(NewOrderQuantity));
            OnPropertyChanged(nameof(CanPlaceOrder));
            OnPropertyChanged(nameof(CanAddToCart));
        }

        private void RemoveCartItem(CartItemViewModel item)
        {
            if (item == null) return;
            CartItems.Remove(item);
            OnPropertyChanged(nameof(CanPlaceOrder));
        }

        private void PlaceOrder()
        {
            var user = _auth.CurrentUser!;

            var requirements = new Dictionary<int, int>();
            foreach (var ci in CartItems)
            {
                var oi = ci.Orderable;
                var qty = ci.Quantity;

                if (!oi.IsMenu)
                {
                    var dish = (Dish)oi.Entity;
                    var neededGrams = qty * dish.PortionQuantity;
                    requirements[dish.DishId] =
                        requirements.GetValueOrDefault(dish.DishId) + neededGrams;
                }
                else
                {
                    var menu = (Menu)oi.Entity;
                    foreach (var mi in menu.MenuItems)
                    {
                        var neededGrams = qty * mi.MenuPortionGrams;
                        requirements[mi.DishId] =
                            requirements.GetValueOrDefault(mi.DishId) + neededGrams;
                    }
                }
            }

            var shortfalls = new List<string>();
            foreach (var (dishId, reqGrams) in requirements)
            {
                var dish = _db.Dishes.Find(dishId)!;
                if (dish.TotalQuantity < reqGrams)
                {
                    var availablePortions = dish.TotalQuantity / dish.PortionQuantity;
                    var neededPortions = (double)reqGrams / dish.PortionQuantity;
                    shortfalls.Add(
                        $"{dish.Name}: în stoc {availablePortions} porții " +
                        $"({dish.TotalQuantity} g), ai nevoie de {neededPortions:N1} porții " +
                        $"({reqGrams} g)"
                    );
                }
            }

            if (shortfalls.Any())
            {
                MessageBox.Show(
                    "Nu se poate plasa comanda din cauza stocului insuficient:\n\n" +
                    string.Join("\n", shortfalls),
                    "Stoc insuficient",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            var windowStart = DateTime.Now.Subtract(_bulkOrderWindow);
            var recentCount = _db.Orders
                .Where(o => o.UserId == user.UserId
                         && o.OrderDate >= windowStart)
                .Count();




            var ord = new Order
            {
                UserId = user.UserId,
                OrderDate = DateTime.Now,
                Status = OrderStatus.Registered,
                Code = Guid.NewGuid().ToString().Split('-')[0],
                OrderItems = new List<OrderItem>()
            };

            foreach (var ci in CartItems)
            {
                var oi = ci.Orderable;
                var qty = ci.Quantity;

                if (!oi.IsMenu)
                {
                    var dishEntity = _db.Dishes.Find(((Dish)oi.Entity).DishId)!;
                    var gramsToRemove = qty * dishEntity.PortionQuantity;
                    dishEntity.TotalQuantity -= gramsToRemove;
                    ord.OrderItems.Add(new OrderItem
                    {
                        Dish = dishEntity,
                        DishId = dishEntity.DishId,
                        Menu = null,
                        MenuId = null,
                        Quantity = qty,
                        PriceAtOrder = dishEntity.Price
                    });

                    dishEntity.TotalQuantity -=
                        qty * dishEntity.PortionQuantity;
                }
                else
                {
                    
                    var menuEntity = _db.Menus
                                       .Include(m => m.MenuItems)
                                         .ThenInclude(mi => mi.Dish)
                                       .Single(m => m.MenuId == oi.Id);

                    ord.OrderItems.Add(new OrderItem
                    {
                        Menu = menuEntity,                
                        MenuId = menuEntity.MenuId,        
                        Dish = null,                      
                        DishId = null,                      
                        Quantity = qty,
                        PriceAtOrder = oi.Price
                    });

                    foreach (var mi in menuEntity.MenuItems)
    {
        var gramsToRemove = qty * mi.MenuPortionGrams;
        mi.Dish.TotalQuantity -= gramsToRemove;
    }
                }
            }

            ord.FoodCost = ord.OrderItems.Sum(x => x.PriceAtOrder * x.Quantity);
            ord.DeliveryFee = ord.FoodCost >= _freeDeliveryThreshold ? 0 : _deliveryFee;
            ord.DiscountAmount = 0;
            if(recentCount >= _bulkOrderCount)
            {
                ord.DiscountAmount += ord.FoodCost * _orderDiscount / 100;
            }
            if (ord.FoodCost >= _orderDiscountThreshold)
            {
                ord.DiscountAmount += ord.FoodCost * _orderDiscount / 100;
            }

            ord.TotalCost = ord.FoodCost + ord.DeliveryFee - ord.DiscountAmount;
            ord.EstimatedDeliveryTime = TimeSpan.FromMinutes(30);

            _db.Orders.Add(ord);
            _db.SaveChanges();

            CartItems.Clear();
            RefreshDisplayedOrders();
        }




        private void RefreshDisplayedOrders()
        {
            DisplayedOrders.Clear();
            IQueryable<Order> q = _db.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .Include(o => o.OrderItems) 
                .ThenInclude(oi => oi.Dish)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Menu)
                .OrderByDescending(o => o.OrderDate);

          
            if (_auth.CurrentUser?.Role == UserRole.Client)
                q = q.Where(o => o.UserId == _auth.CurrentUser.UserId);

            foreach (var o in q)
                DisplayedOrders.Add(new OrderListItemViewModel(o, _config));
        }

        private void Cancel(OrderListItemViewModel vm)
        {
            var ord = _db.Orders.Find(vm.OrderId)!;
            ord.Status = OrderStatus.Cancelled;
            _db.SaveChanges();
            RefreshDisplayedOrders();
        }

        private void ChangeStatus(OrderListItemViewModel vm)
        {
            var ord = _db.Orders.Find(vm.OrderId)!;
            if (vm.NextStatus.HasValue)
            {
                ord.Status = vm.NextStatus.Value;
                _db.SaveChanges();
                RefreshDisplayedOrders();
            }
        }



        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string n = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
       public class OrderableItem
{
    public int      Id            { get; }
    public bool     IsMenu        { get; }  
    public string   CategoryName  { get; }
    public string   DisplayText   { get; }   
    public object   Entity        { get; }   

   
    public decimal  Price         { get; }

    
    public OrderableItem(Dish d)
    {
        Id           = d.DishId;
        IsMenu       = false;
        CategoryName = d.Category!.Name;
        Price        = d.Price;
        DisplayText  = $"{d.Name} ({d.PortionQuantity}g) – {d.Price:C}";
        Entity       = d;
    }

  
    public OrderableItem(Menu m, decimal discountPct)
    {
        Id           = m.MenuId;
        IsMenu       = true;
        CategoryName = m.Category!.Name;

        var raw = m.MenuItems.Sum(mi =>
            mi.Dish.Price
            * mi.MenuPortionGrams
            / (decimal)mi.Dish.PortionQuantity);
        var discounted = Math.Round(raw * (1 - discountPct / 100M), 2);

        Price        = discounted;
        var portionStrings = m.MenuItems
                        .Select(mi => $"{mi.MenuPortionGrams}g")
                        .ToArray();
        var portionsDisplay = string.Join("/", portionStrings);

        DisplayText = $"{m.Name} ({portionsDisplay}) – {discounted:C}";
        Entity       = m;
    }
}

}
