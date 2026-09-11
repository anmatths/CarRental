# Stack
```text
- ASP.NET Core Web API
- .NET 10
- Entity Framework Core
- PostgreSQL
- xUnit
- OpenAPI / Swagger
- Docker
- IMemoryCache
```

# Arquitectura propuesta
```text
CarRental.sln

src/
├── CarRental.Api
├── CarRental.Application
├── CarRental.Domain
└── CarRental.Infrastructure

tests/
├── CarRental.Domain.Tests
├── CarRental.Application.Tests
└── CarRental.IntegrationTests
```