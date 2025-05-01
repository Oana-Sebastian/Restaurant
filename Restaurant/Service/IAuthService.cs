using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Restaurant.Models;

namespace Restaurant.Service
{
    public interface IAuthService
    {
        User? CurrentUser { get; }
        bool Login(string email, string password);
        void Logout();
    }
}
