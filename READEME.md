<!-- Postgres -->
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=your_port;Database=your_database;Username=your_user_name;Password=your_password;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}


<!-- My SQL -->
{
    "ConnectionStrings": {
        "DefaultConnection": "Server=localhost;Port=your_port;Database=your_database;User=your_user_name;Password=your_password;"
    },

    "Logging": {
        "LogLevel": {
            "Default": "Information",
            "Microsoft.AspNetCore": "Warning"
        }
    },

    "AllowedHosts": "*"
}

<!-- Để kết nối bằng mysql thay vì postgres, hãy sửa file 
1. appsettings.json
2. WebApplication1.csproj
3. Program.cs -->