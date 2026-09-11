# Car Rental API

REST API built with .NET to manage airport car rentals. It is designed to be consumed by a frontend such as Angular and supports customer registration, availability checks, rental booking, modification, and cancellation.

## Tech stack

- .NET 10 / ASP.NET Core Web API
- Entity Framework Core and PostgreSQL
- OpenAPI and Swagger UI
- xUnit, Testcontainers, and real PostgreSQL for integration testing
- Docker Compose
- `IMemoryCache`

## Architecture

The solution is organized into four layers:

| Project | Responsibility |
| --- | --- |
| `CarRental.Api` | Controllers, HTTP configuration, OpenAPI, and global exception handling. |
| `CarRental.Application` | Use cases, commands, queries, handlers, DTOs, and persistence/cache abstractions. |
| `CarRental.Domain` | Entities, invariants, and domain exceptions. |
| `CarRental.Infrastructure` | EF Core, PostgreSQL, repositories, and the cache implementation. |

```text
Api
 ↓
Application
 ↓
Domain

Infrastructure → Application / Domain
```

`Domain` does not depend on ASP.NET Core or EF Core. The `Rental` entity encapsulates relevant behavior: creation, period changes, cancellation, and overlap detection.

## CQRS

Commands and queries are explicitly separated, without introducing MediatR. The use cases include:

- Commands: `RegisterCustomerCommand`, `RegisterRentalCommand`, `ModifyRentalCommand`, and `CancelRentalCommand`.
- Queries: `CheckCarAvailabilityQuery`, `GetRentalQuery`, and `GetRentalsQuery`.

## Domain rules and concurrency

### Rental periods

Periods use the `[startDate, endDate)` convention: the start is inclusive and the end is exclusive. Therefore, `01/10 → 05/10` and `05/10 → 10/10` are valid consecutive rentals.

An active rental overlaps a requested period when:

```text
existing.StartDate < requested.EndDate
&& existing.EndDate > requested.StartDate
```

A car cannot have two overlapping active rentals; cancelled rentals do not block availability.

Protection is applied at two levels:

1. The application performs an early validation through `Rental.Overlaps()`.
2. PostgreSQL provides the final guarantee through the `EX_Rentals_ActiveCarDateRange` exclusion constraint, preventing overlapping active periods from being persisted for the same car, even with concurrent requests. The repository maps only PostgreSQL `23P01` violations of that constraint to an availability conflict.

## Cache

`GET /api/cars/availability` uses `IMemoryCache`. The cached handler acts as a **Decorator** over the availability query:

- On a cache hit, it returns the stored result.
- On a cache miss, it runs the query and stores the result for up to three minutes.
- `RegisterRental`, `ModifyRental`, and `CancelRental` invalidate availability after persisting their changes.

Invalidation increments a generation included in the cache key, making prior entries unreachable without scanning the cache. If a read coincides with invalidation, the result is recalculated to avoid returning stale data.

## Error handling

The API uses `IExceptionHandler` and `ProblemDetails` for consistent error responses:

| Status | Meaning |
| --- | --- |
| 400 | Validation or domain errors. |
| 404 | Customer, car, or rental not found. |
| 409 | The car is unavailable. |
| 500 | Unexpected error. |

## API endpoints

| Method | Endpoint | Purpose |
| --- | --- | --- |
| `POST` | `/api/customers` | Register a customer. |
| `GET` | `/api/cars/availability` | Check available cars by period, with optional type/model filters. |
| `POST` | `/api/rentals` | Register a rental. |
| `GET` | `/api/rentals` | List persisted rentals. |
| `GET` | `/api/rentals/{id}` | Get a rental by ID. |
| `PUT` | `/api/rentals/{id}` | Modify an active rental period. |
| `DELETE` | `/api/rentals/{id}` | Cancel a rental. |

Explore the request contracts and execute the endpoints through Swagger in Development.

## Run with Docker

From the `CarRental` directory, start the API and PostgreSQL:

```bash
docker compose up --build
```

- API: [http://localhost:8080](http://localhost:8080)
- Swagger: [http://localhost:8080/swagger](http://localhost:8080/swagger)
- PostgreSQL: `localhost:5432`

Stop the environment while retaining database data:

```bash
docker compose down
```

To also remove the local PostgreSQL volume:

```bash
docker compose down -v
```

Docker uses development-only PostgreSQL credentials (`postgres` / `postgres`). They are not production credentials.

### Development demo cars

In Development, startup applies migrations and seeds the following deterministic demo cars. **Demo Cars are seeded only in Development.** No customers or rentals are created, so availability and rental booking can be tested immediately.

| Type | Model |
| --- | --- |
| Sedan | Toyota Corolla |
| Hatchback | Toyota Yaris |
| Sedan | Honda Civic |
| SUV | Honda CR-V |
| Hatchback | Ford Focus |
| Pickup | Ford Ranger |
| Hatchback | Volkswagen Golf |
| SUV | Volkswagen Taos |

## Run without Docker

Install .NET 10 and make a PostgreSQL instance available. Set the `ConnectionStrings__CarRental` environment variable (or configure the `CarRental` connection string), then run:

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/CarRental.Api
```

The local Development launch profile listens on `http://localhost:5298`; Swagger is then available at `/swagger`.

### Database migrations

At startup, the API applies pending EF Core migrations only in the `Development` environment. Docker sets that environment intentionally, making the local/evaluation setup ready to use. This is a local convenience, not a production-grade migration deployment strategy.

## Testing

The test suite contains:

- Domain unit tests for rental invariants and period overlap behavior.
- Application tests for command/query handlers and cache invalidation.
- API integration tests for endpoints and global error mapping.
- PostgreSQL integration tests using Testcontainers, including the active-rental exclusion constraint.

Run all tests with:

```bash
dotnet test
```

## Assumptions

- **Service:** the supplied model associates `Service[]` with `Car`, but it does not define how a service affects availability. Services are represented in the model; no additional availability rule was inferred.
- **Dates:** the API uses `DateOnly` and `[startDate, endDate)` periods.
- **Cancellation:** canceling changes the rental status to `Cancelled` and retains the record. This is a business operation, not a soft delete.

## AI-assisted development

ChatGPT and OpenAI Codex were used as collaborative development tools for architecture discussion, implementation proposals, code review, test refinement, edge-case analysis, troubleshooting and documentation. They did not replace ownership of the final design or code.

The working approach was: analyze the requirement and risks; discuss a design; request scoped changes; manually review them; run build and tests; refine or reject proposals when needed; and keep the resulting work in atomic commits. No custom agents, skills, or MCP servers were required for the application itself.

The workflow deliberately used conversational analysis with ChatGPT for requirements, architecture, and risk discussion, followed by scoped implementation work with OpenAI Codex. Spec-driven tooling such as OpenSpec was considered, but a lightweight conversational approach was preferred because the challenge scope was compact and its decisions could be reviewed, tested, and documented directly.

Examples of technical judgment applied to AI suggestions:

- **Accepted:** a PostgreSQL exclusion constraint to protect against concurrent overlapping rentals.
- **Modified:** persistence error mapping was narrowed to PostgreSQL SQLSTATE `23P01` and `EX_Rentals_ActiveCarDateRange`, rather than treating every `DbUpdateException` as an availability conflict.
- **Avoided:** generic repositories, distributed cache and additional infrastructure were not added without a concrete need.

AI-assisted code was validated through manual review, `dotnet build`, `dotnet test`, API/integration tests with real PostgreSQL via Testcontainers, Swagger exploration, and the Docker environment.

## Security and future improvements

The repository contains no production credentials. Docker connection values are exclusively for local development.

Possible future work, not currently implemented, includes authentication, authorization, a production migration strategy, distributed caching, observability and soft delete.
