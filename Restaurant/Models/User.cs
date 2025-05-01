using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Restaurant.Models;

namespace Restaurant.Models
{
    public enum UserRole { Guest, Client, Employee }

    public class User
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string Address { get; set; } = "";
        public UserRole Role { get; set; }
        public ICollection<Order>? Orders { get; set; }
    }
}
