using System;
using System.Linq;
using Restaurant.Data;
using Restaurant.Models;
using Restaurant.Service;

public class AuthService : IAuthService
{
    private readonly RestaurantDbContext _db;
    public User? CurrentUser { get; private set; }

    public AuthService(RestaurantDbContext db) => _db = db;

    public bool Login(string email, string password)
    {
        var user = _db.Users.FirstOrDefault(u => u.Email == email.Trim());
        if (user == null) return false;

        var hash = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes(password)
        );
        if (hash != user.PasswordHash) return false;

        if (user.Role == UserRole.Employee
        && !email.EndsWith("@restaurant.com", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        CurrentUser = user;
        return true;
    }

    public void Logout() => CurrentUser = null;
}
