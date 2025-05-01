// App.xaml.cs
using System;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Restaurant.Data;
using Restaurant.ViewModels;
using Restaurant.Service;
using Restaurant.Views;

namespace Restaurant
{
    public partial class App : Application
    {
        // Expose the DI container
        public IServiceProvider ServiceProvider { get; }

        public App()
        {
            // 1. Build configuration
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // 2. Register everything
            var services = new ServiceCollection()
                // Configuration & DbContext
                .AddSingleton<IConfiguration>(config)
                .AddDbContext<RestaurantDbContext>(opts =>
                    opts.UseSqlServer(config.GetConnectionString("RestaurantDb")))

                // ViewModels
                .AddTransient<RegisterViewModel>()
                .AddTransient<LoginViewModel>()
                //.AddTransient<SearchViewModel>()
                //.AddTransient<MenuViewModel>()
                //.AddTransient<OrderViewModel>()
                //.AddTransient<EmployeeDashboardViewModel>()
                // … other VMs …

                // UserControls
                .AddTransient<LoginControl>()
                .AddTransient<RegisterControl>()

                // MainWindow as singleton + navigation
                .AddSingleton<MainWindow>()
                .AddSingleton<MainWindowViewModel>()
                .AddSingleton<INavigationService>(sp =>
                    sp.GetRequiredService<MainWindow>())

                // App services (e.g. authentication, order service, etc.)
                .AddScoped<IAuthService, AuthService>()
                // …
                .AddScoped<LoginViewModel>()
                .AddScoped<RegisterViewModel>()
                
                //.AddScoped<MainMenuViewModel>()
            ;

            ServiceProvider = services.BuildServiceProvider();
        }


        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            using (var scope = ServiceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<RestaurantDbContext>();

                try
                {
                    if (dbContext.Database.CanConnect())
                    {
                        System.Diagnostics.Debug.WriteLine("✅ EF Core can connect to the database.");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("❌ EF Core could not connect to the database.");
                    }

                    // Optional: create the DB if it doesn't exist
                    dbContext.Database.EnsureCreated();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Database error: {ex.Message}");
                    MessageBox.Show($"Database connection failed:\n{ex.Message}", "Database Error",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            var main = ServiceProvider.GetRequiredService<MainWindow>();
            main.DataContext = ServiceProvider.GetRequiredService<MainWindowViewModel>();
            main.Show();
        }
    }
}
