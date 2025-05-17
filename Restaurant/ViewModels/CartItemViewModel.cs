using Restaurant.ViewModels;

public class CartItemViewModel
{
    public string DisplayText { get; }
    public OrderableItem Orderable { get; }
    public int Quantity { get; private set; }
    public decimal Subtotal { get; private set; } 

    private readonly OrderableItem _oi;

    public CartItemViewModel(OrderableItem oi, int qty)
    {
        _oi = oi;
        DisplayText = oi.DisplayText;
        Orderable = oi;
        Increase(qty);
    }

    public void Increase(int delta)
    {
        Quantity += delta;
        Subtotal = Quantity * Orderable.Price; 
    }
}
