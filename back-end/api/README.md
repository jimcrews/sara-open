# .NET Core on Linux

### Install .NET Core for Arch Linux

```bash
yay -S dotnet-runtime
yay -S dotnet-sdk
sudo pacman -S aspnet-runtime
```

<br />

### Commands to get started:

```bash
# Create solution
dotnet new sln --name sara-open

# Create API Project
mkdir api
cd api
dotnet new "webapi"

# Add Project to Solution
dotnet sln add api/api.csproj

# Run local
dotnet run
```

<br />

### Azure setup

1. Create App Service
2. Configure for .NET Core 3, running on Linux
3. Setup continuous deployment via Github

<br />

### Add files not kept in source control (details below)

- appsettings.json
- appsettins.Development.json

```json
{
  "ConnectionStrings": {
    "Sara": "Connection String for SARA config database here"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "AllowedHosts": "*"
}
```
