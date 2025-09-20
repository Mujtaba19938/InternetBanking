# 🏦 Internet Banking Application

A comprehensive ASP.NET Core MVC web application that simulates a modern internet banking system with role-based access control, secure authentication, and full banking functionality.

## ✨ Features

### 🔐 Authentication & Authorization
- **User Registration & Login**: Secure user authentication with ASP.NET Core Identity
- **Role-Based Access Control**: Separate interfaces for Users and Administrators
- **Session Management**: Automatic session expiration on role changes
- **Transaction Password**: Additional security layer for financial transactions

### 👤 User Features
- **Account Dashboard**: Overview of all accounts with balances
- **Fund Transfers**: Secure money transfers between accounts
- **Transaction History**: Complete transaction records with filtering
- **Service Requests**: Submit and track support requests
- **Account Management**: View account details and statements
- **Profile Management**: Update personal information and transaction passwords

### 👨‍💼 Admin Features
- **Admin Dashboard**: Centralized admin control panel
- **User Management**: View and manage all users
- **Role Management**: Change user roles with automatic session expiration
- **Service Request Management**: Respond to user service requests
- **Custom Queries**: Execute custom SQL queries for data analysis
- **System Monitoring**: Overview of system activity

### 🛡️ Security Features
- **Role Separation**: Admins cannot access user portal and vice versa
- **Input Validation**: Comprehensive form validation
- **SQL Injection Protection**: Parameterized queries
- **Password Hashing**: Secure password storage
- **Session Security**: Automatic logout on unauthorized access

## 🚀 Getting Started

### Prerequisites
- .NET 8.0 SDK
- SQL Server (LocalDB or full instance)
- Git

### Installation

1. **Clone the repository**
   ```bash
   git clone <your-repository-url>
   cd InternetBanking
   ```

2. **Install Entity Framework tools**
   ```bash
   dotnet tool install --global dotnet-ef
   ```

3. **Update the database**
   ```bash
   dotnet ef database update
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

5. **Access the application**
   - User Portal: http://localhost:5000
   - Admin Panel: http://localhost:5000/admin

## 📁 Project Structure

```
InternetBanking/
├── Controllers/          # MVC Controllers
│   ├── AccountController.cs
│   ├── AdminController.cs
│   ├── HomeController.cs
│   ├── TransactionController.cs
│   └── ...
├── Models/              # Data Models
│   ├── Account.cs
│   ├── ApplicationUser.cs
│   ├── Transaction.cs
│   └── ViewModels/
├── Views/               # Razor Views
│   ├── Admin/          # Admin-specific views
│   ├── Home/           # User dashboard views
│   └── Shared/         # Layout files
├── Data/               # Database context
├── Filters/            # Custom authorization filters
├── Migrations/         # Entity Framework migrations
└── wwwroot/           # Static files (CSS, JS, images)
```

## 🔧 Configuration

### Database Connection
Update the connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=InternetBankingDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

### User Roles
The application automatically creates two roles:
- **User**: Regular banking customers
- **Admin**: System administrators

## 🎯 Usage

### For Users
1. Register a new account at `/Account/Register`
2. Login at `/Account/Login`
3. Access your dashboard to view accounts and perform transactions
4. Use the navigation menu to access different features

### For Administrators
1. Register as an admin at `/admin` (first admin user)
2. Login to access the admin panel
3. Manage service requests
4. Execute custom queries for data analysis

## 🛠️ Development

### Adding New Features
1. Create models in the `Models/` directory
2. Add controllers in the `Controllers/` directory
3. Create views in the appropriate `Views/` subdirectory
4. Update the database with new migrations

### Database Changes
```bash
# Create a new migration
dotnet ef migrations add MigrationName

# Update the database
dotnet ef database update
```

## 🔒 Security Considerations

- **Never commit sensitive data** like connection strings or API keys
- **Use HTTPS** in production environments
- **Regularly update dependencies** to patch security vulnerabilities
- **Implement proper logging** for security monitoring
- **Use strong passwords** and enforce password policies

## 📝 License

This project is for educational purposes. Please ensure compliance with local banking regulations before using in production.

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## 📞 Support

For issues and questions:
- Create an issue in the GitHub repository
- Check the documentation in the `/docs` folder
- Review the code comments for implementation details

---

**Note**: This is a demonstration application and should not be used for actual banking operations without proper security audits and compliance checks. 