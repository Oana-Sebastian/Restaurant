using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

        // thresholds & fees from config
        private readonly decimal _freeDeliveryThreshold;
        private readonly decimal _deliveryFee;
        private readonly decimal _orderDiscountThreshold;
        private readonly int _bulkOrderCount;
        private readonly TimeSpan _bulkOrderWindow;
        private readonly decimal _bulkOrderDiscount;

        public ObservableCollection<OrderableItem> MenuItemsForOrdering { get; }
        public OrderableItem SelectedOrderItem { get; set; }
        public string NewOrderQuantity { get; set; } = "1";
        public ICommand AddToCartCommand { get; }
        public ICommand PlaceOrderCommand { get; }

        // in‐memory cart
        //private readonly ObservableCollection<(MenuItem mi, int qty)> _cart
        //    = new ObservableCollection<(MenuItem, int)>();

        private readonly ObservableCollection<(OrderableItem item, int qty)> _cart
    = new ObservableCollection<(OrderableItem, int)>();

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

        public ICommand CancelOrderCommand { get; }
        public ICommand ChangeStatusCommand { get; }

        public OrderViewModel(RestaurantDbContext db,
                              IAuthService auth,
                              IConfiguration config)
        {
            _db = db; _auth = auth; _config = config;

            var discountPct = _config.GetValue<int>("Settings:MenuDiscountPercent");

            // Eager‐load categories → dishes + menus
            var cats = db.Categories
                // pull in dishes (no need to fetch their .Category again)
                .Include(c => c.Dishes)
                    .ThenInclude(d => d.DishAllergens)   // if you need dish allergens
                        .ThenInclude(da => da.Allergen)
                .Include(c => c.Dishes)
                    .ThenInclude(d => d.Images)
                // pull in menus + their dishes (no need to re-include dish.Category)
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
            _orderDiscountThreshold = config.GetValue<decimal>("Settings:FreeDeliveryThreshold");
            _bulkOrderCount = config.GetValue<int>("Settings:BulkOrderCountThreshold");
            _bulkOrderWindow = TimeSpan.FromHours(
                                         config.GetValue<int>("Settings:BulkOrderTimeWindowHours"));
            _bulkOrderDiscount = config.GetValue<decimal>("Settings:BulkOrderDiscountPercent");

            // load menu items for “place order” dropdown
            //MenuItemsForOrdering = new ObservableCollection<MenuItem>(
            //    db.MenuItems
            //      .Include(mi => mi.Dish)
            //      .ThenInclude(d => d.Category)
            //      .ToList()
            //);

            // Flatten into one list
            var orderables = new List<OrderableItem>();
            foreach (var c in cats)
            {
                // dishes
                foreach (var d in c.Dishes)
                    orderables.Add(new OrderableItem(d));
                // menus
                foreach (var m in c.Menus)
                    orderables.Add(new OrderableItem(m, discountPct));
            }

            MenuItemsForOrdering = new ObservableCollection<OrderableItem>(orderables);

            AddToCartCommand = new RelayCommand(_ => AddToCart(), _ => CanAddToCart);
            PlaceOrderCommand = new RelayCommand(_ => PlaceOrder(), _ => CanPlaceOrder);

            CancelOrderCommand = new RelayCommand(o => Cancel((OrderListItemViewModel)o));
            ChangeStatusCommand = new RelayCommand(o => ChangeStatus((OrderListItemViewModel)o));

            RefreshDisplayedOrders();
        }

            private void AddToCart()
        {
            if (SelectedOrderItem == null) return;
            if (!int.TryParse(NewOrderQuantity, out var q) || q <= 0) return;

            // 1) Check stock for this single addition
            //    (reuse the same logic from PlaceOrder’s requirement map,
            //     but only for this OrderableItem and this q)
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

            // 2) Add or update an existing CartItemViewModel
            var existing = CartItems.FirstOrDefault(ci => ci.DisplayText == SelectedOrderItem.DisplayText);
            if (existing != null)
            {
                existing.Increase(q);
            }
            else
            {
                CartItems.Add(new CartItemViewModel(SelectedOrderItem, q));
            }

            // 3) Reset the entry and update bindings
            NewOrderQuantity = "1";
            OnPropertyChanged(nameof(NewOrderQuantity));
            OnPropertyChanged(nameof(CanPlaceOrder));
            OnPropertyChanged(nameof(CanAddToCart));
        }

        private void PlaceOrder()
        {
            var user = _auth.CurrentUser!;

            // 1) Build a flat list of (DishId → required portions)
            var requirements = new Dictionary<int, int>();
            foreach (var ci in CartItems)
            {
                var oi = ci.Orderable;   // assuming you expose the OrderableItem in your CartItemViewModel
                var qty = ci.Quantity;

                if (!oi.IsMenu)
                {
                    var dish = (Dish)oi.Entity;
                    requirements[dish.DishId] =
                        requirements.GetValueOrDefault(dish.DishId) + qty;
                }
                else
                {
                    var menu = (Menu)oi.Entity;
                    foreach (var mi in menu.MenuItems)
                    {
                        // calculate how many portions of this dish are needed
                        var portionCount = (int)Math.Ceiling(
                           qty * mi.MenuPortionGrams / (double)mi.Dish.PortionQuantity);
                        requirements[mi.DishId] =
                            requirements.GetValueOrDefault(mi.DishId) + portionCount;
                    }
                }
            }

            // 2) Check stock
            var shortfalls = new List<string>();
            foreach (var (dishId, reqPortions) in requirements)
            {
                var dish = _db.Dishes.Find(dishId)!;
                var availablePortions = dish.TotalQuantity / dish.PortionQuantity;
                if (availablePortions < reqPortions)
                {
                    shortfalls.Add(
                        $"{dish.Name}: ai cerut {reqPortions} portie(i), în stoc doar {availablePortions}"
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

            // 3) All good—build the Order
            var ord = new Order
            {
                UserId = user.UserId,
                OrderDate = DateTime.Now,
                Status = OrderStatus.Registered,
                Code = Guid.NewGuid().ToString().Split('-')[0],
                OrderItems = new List<OrderItem>()
            };

            // 4) Populate OrderItems from CartItems
            foreach (var ci in CartItems)
            {
                var oi = ci.Orderable;
                var qty = ci.Quantity;

                if (!oi.IsMenu)
                {
                    var dish = (Dish)oi.Entity;
                    ord.OrderItems.Add(new OrderItem
                    {
                        DishId = dish.DishId,
                        Quantity = qty,
                        PriceAtOrder = dish.Price
                    });
                }
                else
                {
                    var menu = (Menu)oi.Entity;
                    foreach (var mi in menu.MenuItems)
                    {
                        var portions = (int)Math.Ceiling(
                            qty * mi.MenuPortionGrams / (double)mi.Dish.PortionQuantity);
                        ord.OrderItems.Add(new OrderItem
                        {
                            DishId = mi.DishId,
                            Quantity = portions,
                            PriceAtOrder = mi.Dish.Price
                        });
                    }
                }
            }

            // 5) Compute costs / discounts / delivery fee
            //    … your existing logic here …

            _db.Orders.Add(ord);

            // 6) Deduct stock
            foreach (var oi in ord.OrderItems)
            {
                var dish = _db.Dishes.Find(oi.DishId)!;
                dish.TotalQuantity -= oi.Quantity * dish.PortionQuantity;
            }

            _db.SaveChanges();

            // 7) Clear the cart and refresh UI
            CartItems.Clear();
            RefreshDisplayedOrders();
        }




        private void RefreshDisplayedOrders()
        {
            DisplayedOrders.Clear();
            IQueryable<Order> q = _db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Dish)
                .OrderByDescending(o => o.OrderDate);

            // client sees only own orders
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
    public bool     IsMenu        { get; }   // true=menu, false=dish
    public string   CategoryName  { get; }
    public string   DisplayText   { get; }   // shown in the ComboBox
    public object   Entity        { get; }   // underlying Dish or Menu

    // ← New: unified price-per-portion (or per-menu)
    public decimal  Price         { get; }

    // Constructor for a single Dish
    public OrderableItem(Dish d)
    {
        Id           = d.DishId;
        IsMenu       = false;
        CategoryName = d.Category!.Name;
        Price        = d.Price;
        DisplayText  = $"{d.Name} ({d.PortionQuantity}g) – {d.Price:C}";
        Entity       = d;
    }

    // Constructor for a Menu (discounted total)
    public OrderableItem(Menu m, decimal discountPct)
    {
        Id           = m.MenuId;
        IsMenu       = true;
        CategoryName = m.Category!.Name;

        // compute raw total, then discount
        var raw = m.MenuItems.Sum(mi =>
            mi.Dish.Price
            * mi.MenuPortionGrams
            / (decimal)mi.Dish.PortionQuantity);
        var discounted = Math.Round(raw * (1 - discountPct / 100M), 2);

        Price        = discounted;
        DisplayText  = $"{m.Name} – {discounted:C}";
        Entity       = m;
    }
}

}
