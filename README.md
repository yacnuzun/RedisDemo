# RedisDemo

A focused ASP.NET Core Web API project demonstrating the **cache-aside pattern** with Redis, built on top of PostgreSQL and EF Core, using a **modular monolith** architecture.

This project was built as a hands-on demonstration of how to correctly integrate a distributed cache into a real API — not just "connect to Redis," but implement the actual pattern (cache hit/miss, TTL, invalidation) the way it's done in production systems.

---

## What this project demonstrates

- **Cache-aside (lazy loading) pattern** — the business layer checks the cache first, falls back to the database on a miss, and populates the cache afterward.
- **Cache invalidation** — on update/delete, the corresponding cache entry is explicitly removed to prevent stale reads.
- **TTL-based expiration** — cached entries expire automatically after a configurable duration.
- **Clean separation of concerns** — the business layer (`InvoiceService`) depends only on a generic `ICacheService` abstraction, with zero direct dependency on `StackExchange.Redis`. Swapping Redis for another cache provider would require no changes outside the `Shared/Caching` module.
- **Modular monolith structure** — code is organized by business module (`Modules/Invoices`) rather than purely by technical layer, with clear internal boundaries (`Domain`, `Application`, `Infrastructure`, `Api`) that could be extracted into a separate service later without a rewrite.

---

## Tech Stack

| Layer | Technology |
|---|---|
| API | ASP.NET Core 8 Web API |
| Database | PostgreSQL |
| ORM | Entity Framework Core (Npgsql provider) |
| Cache | Redis (via `StackExchange.Redis`) |
| API Documentation | Swagger / Swashbuckle |
| Containerization | Docker (Redis) |

---

## Architecture

```
WebApiUI/
├── Modules/
│   └── Invoices/
│       ├── Api/              → Controllers (HTTP concerns only)
│       ├── Application/      → Business logic, cache-aside orchestration
│       ├── Domain/           → Plain entity, no external dependencies
│       └── Infrastructure/   → (reserved for module-specific data access)
├── Shared/
│   ├── Caching/              → ICacheService / RedisCacheService (generic, reusable)
│   └── Persistence/          → AppDbContext
└── Program.cs                → DI composition root
```

**Why modular monolith instead of microservices:** for a single-developer, single-entity demo, microservices would add infrastructure overhead (service discovery, distributed transactions, separate deployments) without solving a real problem. The module boundaries here are designed so that if a module *does* need to scale independently in the future, it can be extracted with minimal friction — without paying that cost upfront.

---

## Cache-Aside Flow (the core of this demo)

```
GET /api/invoices/{id}
        │
        ▼
  Check Redis (ICacheService.GetAsync)
        │
   ┌────┴────┐
   │  HIT    │  MISS
   ▼         ▼
Return    Query PostgreSQL
cached    (EF Core)
value          │
               ▼
        Write result to Redis
        (with TTL)
               │
               ▼
           Return value
```

On `PUT`/`DELETE`, the corresponding `invoice:{id}` key is removed from Redis, so the next `GET` is guaranteed to fetch fresh data from the database.

---

## Getting Started

### Prerequisites
- .NET 8 SDK
- Docker (for Redis)
- PostgreSQL (local instance)

### 1. Start Redis

```bash
docker run -d --name redis-demo -p 6379:6379 redis:latest
```

### 2. Configure connection strings

Update `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379",
    "Postgres": "Host=localhost;Port=5432;Database=redisdemo;Username=postgres;Password=YOUR_PASSWORD"
  }
}
```

### 3. Apply database migrations

```bash
dotnet ef database update
```

### 4. Run the API

```bash
dotnet run
```

Swagger UI will be available at `https://localhost:{port}/swagger`.

---

## API Endpoints

| Method | Route | Description |
|---|---|---|
| GET | `/api/invoices` | List all invoices (not cached — see notes below) |
| GET | `/api/invoices/{id}` | Get a single invoice — **cache-aside applied here** |
| POST | `/api/invoices` | Create a new invoice |
| PUT | `/api/invoices/{id}` | Update an invoice — invalidates cache |
| DELETE | `/api/invoices/{id}` | Delete an invoice — invalidates cache |

---

## Design Decisions & Trade-offs

- **Why not cache the list endpoint?** Caching collections introduces significantly more complex invalidation logic (any single create/update/delete would need to invalidate the list cache too). This was intentionally left out of scope to keep the demo focused on the core single-entity cache-aside pattern.
- **Why no generic repository / Unit of Work?** With EF Core, `DbContext` already *is* the Unit of Work, and `DbSet<T>` already behaves like a repository. Adding an extra repository abstraction here would introduce indirection without solving a real problem at this scale — a deliberate, documented decision rather than an oversight.
- **Not included (intentionally out of scope):** distributed locking, cache stampede protection, write-through/write-behind strategies. These solve problems (concurrent writers, thundering herd) that don't yet exist at this project's scale, and are noted here as a natural "next step" rather than gaps.

---

## License

MIT