# ShopNew E-commerce Application

ASP.NET Core e-commerce application with PostgreSQL database, JWT authentication, and Stripe payment integration.

## Features
- User authentication and authorization
- Product management
- Shopping cart functionality  
- Order processing
- Stripe payment integration
- Admin dashboard
- Docker deployment ready

## Tech Stack
- ASP.NET Core 8.0
- Entity Framework Core with PostgreSQL
- JWT Authentication
- BCrypt password hashing
- Stripe payment processing
- Docker & Docker Compose

## Quick Start with Docker

1. Clone the repository
2. Copy environment file:
   ```bash
   cp .env.example .env
   ```
3. Edit `.env` file with your values
4. Run with Docker:
   ```bash
   docker-compose up -d
   ```

## Local Development

1. Install .NET 8.0 SDK
2. Install PostgreSQL
3. Update connection string in `appsettings.json`
4. Run migrations:
   ```bash
   dotnet ef database update
   ```
5. Start the application:
   ```bash
   dotnet run
   ```

## Environment Variables

| Variable | Description | Required |
|----------|-------------|---------|
| `POSTGRES_PASSWORD` | Database password | Yes |
| `JWT_KEY` | JWT signing key (32+ chars) | Yes |
| `STRIPE_SECRET_KEY` | Stripe secret key | For payments |
| `STRIPE_PUBLISHABLE_KEY` | Stripe publishable key | For payments |

Website: dunglt.shop
