# UrbanX Admin Application

A Blazor WebAssembly admin portal for managing the UrbanX platform.

## Features

- **Dashboard**: Overview of system statistics (tenants, customers, orders, revenue)
- **Tenant Management**: List, edit, lock/unlock tenants with different registration levels (Basic, Pro, Business)
- **Customer Management**: List, edit, lock, and delete customers
- **Category Management**: Manage product categories
- **Product Approval**: Review and approve/reject products submitted by tenants
- **Shipping Methods**: Manage available shipping options
- **Payment Methods**: Configure payment providers and settings
- **Orders**: View all orders across the platform
- **Messages**: Send messages to tenants

## Technology Stack

- .NET 10
- Blazor WebAssembly
- Microsoft FluentUI Components
- OpenID Connect authentication (Keycloak)

## Running the Application

### With .NET Aspire (Recommended)

From the repository root:

```bash
cd src/AppHost/UrbanX.AppHost
dotnet run
```

The admin application will be available through the Aspire Dashboard.

### Standalone

```bash
cd src/Frontend/UrbanX.Admin/UrbanX.Admin
dotnet run
```

## Authentication

The admin application requires authentication via Keycloak with the `admin` role.

### Test Admin Account

- **Username:** `admin@urbanx.com`
- **Password:** `Admin123!`

## Configuration

Update `appsettings.json` in the `UrbanX.Admin.Client/wwwroot` directory:

```json
{
  "Oidc": {
    "Authority": "http://localhost:8080/realms/urbanx",
    "ClientId": "urbanx-admin"
  },
  "GatewayUrl": "http://localhost:5000"
}
```

## Note

This is the initial UI implementation with mock data. Backend API integration is pending.
