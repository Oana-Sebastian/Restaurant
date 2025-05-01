using System.Collections.Generic;
using System.Configuration;
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Restaurant.Models;

namespace Restaurant.Data
{
    public class RestaurantDbContext : DbContext
    {
        private readonly IConfiguration _config;
        public RestaurantDbContext(DbContextOptions<RestaurantDbContext> options, IConfiguration config)
            : base(options)
        {
            _config = config;
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Dish> Dishes => Set<Dish>();
        public DbSet<Allergen> Allergens => Set<Allergen>();
        public DbSet<DishAllergen> DishAllergens => Set<DishAllergen>();
        public DbSet<Menu> Menus => Set<Menu>();
        public DbSet<MenuItem> MenuItems => Set<MenuItem>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Dish-Allergen: many-to-many via DishAllergen
            modelBuilder.Entity<DishAllergen>()
                .HasKey(da => new { da.DishId, da.AllergenId });

            modelBuilder.Entity<DishAllergen>()
                .HasOne(da => da.Dish)
                .WithMany(d => d.DishAllergens)
                .HasForeignKey(da => da.DishId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DishAllergen>()
                .HasOne(da => da.Allergen)
                .WithMany(a => a.DishAllergens)
                .HasForeignKey(da => da.AllergenId)
                .OnDelete(DeleteBehavior.Restrict);

            // Menu-Item (MenuItem): many-to-many via MenuItem
            modelBuilder.Entity<MenuItem>()
                .HasKey(mi => new { mi.MenuId, mi.DishId });

            modelBuilder.Entity<MenuItem>()
                .HasOne(mi => mi.Menu)
                .WithMany(m => m.MenuItems)
                .HasForeignKey(mi => mi.MenuId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MenuItem>()
                .HasOne(mi => mi.Dish)
                .WithMany(d => d.MenuItems)
                .HasForeignKey(mi => mi.DishId)
                .OnDelete(DeleteBehavior.Restrict);

            // Order-Item (OrderItem): one-to-many from Order, one-to-many from Dish
            modelBuilder.Entity<OrderItem>()
                .HasKey(oi => new { oi.OrderId, oi.DishId });

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Dish)
                .WithMany(d => d.OrderItems)
                .HasForeignKey(oi => oi.DishId)
                .OnDelete(DeleteBehavior.Restrict);

            // Dish-Category: many dishes in one category
            modelBuilder.Entity<Dish>()
                .HasOne(d => d.Category)
                .WithMany(c => c.Dishes)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Order-User: one user places many orders
            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Optional: configure default schema/table/column naming conventions here if needed
        }

    }
}
