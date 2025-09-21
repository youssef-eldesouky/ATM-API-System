# ATM API System

A robust ASP.NET Core Web API for managing ATM operations, customer accounts, transactions, and operator controls. Built with .NET 8, Entity Framework Core, JWT authentication, and Swagger documentation.

## Features

- **Customer & Account Management:** Create, view, and manage customers, accounts, and cards.
- **ATM Inventory:** Track and reconcile ATM cash inventory.
- **Transactions:** Deposit, withdraw, and view transaction history.
- **Operator Controls:** Operator endpoints for reconciliation, transaction export, security logs, and seeding.
- **Authentication:** Secure JWT-based authentication for users and operators.
- **Swagger UI:** Interactive API documentation and testing.

## Technologies

- ASP.NET Core (.NET 8)
- Entity Framework Core (SQL Server)
- AutoMapper
- JWT Authentication
- Swagger (OpenAPI)
- Newtonsoft.Json

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (local or cloud)
- Visual Studio 2022 (recommended)

### Setup

1. **Clone the repository:**
- git clone https://github.com/yourusername/atm-api-system.git cd atm-api-system

2. **Configure the database:**
- Update the `DefaultConnection` string in `appsettings.json` to point to your SQL Server instance.

3. **JWT Settings:**
- Set your JWT `Key`, `Issuer`, and `Audience` in `appsettings.json` under the `Jwt` section.

4. **Restore NuGet packages:**
- dotnet restore


5. **Run database migrations and seed data:**
 - The app will automatically apply migrations and seed initial data on startup.

6. **Run the application:**
 - dotnet run
  
- The API will be available at `https://localhost:5001` (or your configured port).

7. **Access Swagger UI:**
- Navigate to `https://localhost:5001/swagger` for interactive API documentation.

## API Overview

### Authentication

- `POST /api/auth/login` - Obtain JWT token.

### Customer & Account

- `GET /api/account/{id}` - Get account details.
- `POST /api/account/deposit` - Deposit funds.
- `POST /api/account/withdraw` - Withdraw funds.

### Operator Endpoints

- `POST /api/operator/atm/reconcile` - Reconcile ATM cash.
- `GET /api/operator/transactions` - View transactions.
- `GET /api/operator/security-logs` - View security logs.
- `POST /api/operator/seed` - Seed customer/account.
- `GET/POST /api/operator/export/transactions` - Export transactions as CSV.

### ATM Inventory

- `GET /api/atm/inventory` - View ATM cash inventory.

## Seeding

- On first run, the app seeds sample data: ATM inventory, a customer, accounts, card, transactions, and an operator account (`username: operator`, password: `Operator!23`).

## Development

- Uses EF Core migrations for schema updates.
- AutoMapper for DTO mapping.
- JWT for secure authentication.
- Swagger for API exploration.


---

**Note:** Change default credentials and secrets before deploying to production.
