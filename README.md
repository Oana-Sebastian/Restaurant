# Restaurant Management System 🍽️

A comprehensive restaurant management application built with **C#**, **WPF**, and **Entity Framework Core**. This system provides a complete solution for managing restaurant operations including menus, orders, dishes, and user authentication.

## 📋 Table of Contents

- [Features](#features)
- [Technologies](#technologies)
- [Architecture](#architecture)
- [Getting Started](#getting-started)
- [Database Models](#database-models)
- [User Roles](#user-roles)
- [Configuration](#configuration)
- [Project Structure](#project-structure)
- [Screenshots](#screenshots)

## ✨ Features

### Customer Features
- **Menu Browsing**: View available dishes and menus with images, prices, and allergen information
- **Order Management**: Place orders, track order status, and view order history
- **User Registration & Login**: Secure authentication system with password hashing
- **Shopping Cart**: Add items to cart with customizable quantities
- **Menu Discounts**: Automatic discount calculation for menu orders
- **Delivery Fee Management**: Automatic calculation with free delivery threshold

### Employee Features
- **Dashboard**: Dedicated employee dashboard for management tasks
- **Dish Management**: Add, edit, and manage dishes with images and allergen tracking
- **Menu Management**: Create and edit menus with custom portions from available dishes
- **Order Processing**: Update order status (Registered → Preparing → InTransit → Delivered)
- **Order Cancellation**: Cancel orders in registered status

### Business Logic
- **Dynamic Pricing**: Menu items automatically get discount percentage applied
- **Bulk Order Discounts**: Automatic discounts for qualifying bulk orders
- **Free Delivery**: Automatic free delivery for orders above threshold
- **Allergen Tracking**: Complete allergen management and display
- **Availability Status**: Real-time dish and menu availability tracking

## 🛠️ Technologies

- **.NET 8.0** (Windows)
- **WPF (Windows Presentation Foundation)** - UI Framework
- **Entity Framework Core 8.0.5** - ORM
- **SQL Server** - Database
- **Microsoft.Extensions.DependencyInjection** - Dependency Injection
- **Microsoft.Extensions.Configuration** - Configuration Management
- **MVVM Pattern** - Architecture Pattern

## 🏗️ Architecture

The application follows the **MVVM (Model-View-ViewModel)** design pattern with dependency injection:

```
Restaurant/
├── Models/          # Data models (User, Dish, Menu, Order, etc.)
├── ViewModels/      # View models with business logic
├── Views/           # WPF user controls and windows
├── Data/            # Database context (RestaurantDbContext)
├── Service/         # Services (AuthService, NavigationService)
├── Helpers/         # Helper classes (Commands, utilities)
└── Images/          # Application images and assets
```

## 🚀 Getting Started

### Prerequisites

- **.NET 8.0 SDK** or later
- **SQL Server** (LocalDB, Express, or full version)
- **Visual Studio 2022** (recommended) or VS Code

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/Oana-Sebastian/Restaurant.git
   cd Restaurant
   ```

2. **Configure the database connection**
   
   Create an `appsettings.json` file in the `Restaurant` project directory:
   ```json
   {
     "ConnectionStrings": {
       "RestaurantDb": "Server=(localdb)\\mssqllocaldb;Database=RestaurantDb;Trusted_Connection=True;"
     },
     "Settings": {
       "MenuDiscountPercent": 10,
       "FreeDeliveryThreshold": 100.00,
       "DeliveryFee": 15.00,
       "OrderDiscountThreshold": 200.00,
       "BulkOrderCountThreshold": 5,
       "BulkOrderTimeWindowHours": 24,
       "OrderDiscountPercent": 5
     }
   }
   ```

3. **Restore dependencies**
   ```bash
   dotnet restore
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

   The database will be automatically created on first run using `EnsureCreated()`.

## 📊 Database Models

### Core Models

- **User**: Customer and employee information with role-based access
  - Roles: `Guest`, `Client`, `Employee`
  
- **Category**: Dish and menu categorization

- **Dish**: Individual food items with:
  - Name, description, price, portion size
  - Images (multiple per dish)
  - Allergen associations
  - Availability status

- **Menu**: Pre-configured meal combinations with:
  - Multiple dishes with custom portions
  - Automatic discount pricing
  - Category association

- **Order**: Customer orders with:
  - Order items (dishes or menus)
  - Status tracking (Registered → Preparing → InTransit → Delivered)
  - Automatic delivery fee and discount calculation
  - Unique order codes

- **Allergen**: Food allergens with many-to-many relationship to dishes

## 👥 User Roles

### Guest
- Browse menus and dishes
- View dish details and allergen information

### Client (Registered User)
- All Guest features
- Place and track orders
- View order history
- Manage personal information

### Employee
- Access to employee dashboard
- Manage dishes (CRUD operations)
- Manage menus (CRUD operations)
- Process orders (update status)
- Cancel orders
- View all orders and customers

## ⚙️ Configuration

The application uses `appsettings.json` for configuration:

| Setting | Description | Default |
|---------|-------------|---------|
| `MenuDiscountPercent` | Discount percentage for menu orders | 10% |
| `FreeDeliveryThreshold` | Minimum order for free delivery | 100.00 |
| `DeliveryFee` | Standard delivery fee | 15.00 |
| `OrderDiscountThreshold` | Minimum order for discount eligibility | 200.00 |
| `BulkOrderCountThreshold` | Number of orders for bulk discount | 5 |
| `BulkOrderTimeWindowHours` | Time window for bulk order counting | 24 hours |
| `OrderDiscountPercent` | Discount for qualifying bulk orders | 5% |


## 📝 Key Features Explained

### Authentication System
- Secure password hashing
- Role-based access control
- Session management

### Menu Management
- Create menus with minimum 2 dishes
- Custom portion sizes per dish
- Automatic price calculation with discounts
- Category assignment

### Order Processing
- Real-time order status updates
- Automatic delivery fee calculation
- Bulk order discount detection
- Order history tracking
- 30-minute ETA calculation

### Search Functionality
- Filter dishes and menus by name
- Category-based filtering
- Allergen information display

## 🔒 Security

- Passwords are hashed before storage
- Role-based authorization
- SQL Server integrated security support

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 👨‍💻 Author

**Oana-Sebastian**
- GitHub: [@Oana-Sebastian](https://github.com/Oana-Sebastian)

## 📞 Support

For support, please open an issue in the GitHub repository.
