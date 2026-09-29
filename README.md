# e-commerce

A wallet-paid shop. Customers browse a catalogue, fill a cart and check out against a prepaid
balance; admins manage products and bulk-update stock by uploading a CSV/Excel file that is
processed in the background. Order confirmations and upload reports go out by email.

**Stack:** .NET 10 · ASP.NET Core · MediatR CQRS · FluentValidation · EF Core → SQL Server 2022 ·
JWT bearer · `Channel<T>` + `BackgroundService` · React 19 + Redux Toolkit + Tailwind (Vite)

## Quick start

Needs Docker and a repo-root `.env` with the SQL Server SA password:

```bash
echo 'MSSQL_SA_PASSWORD=<a-strong-password>' > .env
```

### Everything in one command

Brings up SQL Server, applies EF Core migrations, loads the seed data, then starts the API, the
frontend and a Mailpit mail catcher:

```bash
docker compose -f docker-compose.full.yml up -d --build
```

| | |
|---|---|
| Frontend | http://localhost:5173 |
| API + Swagger | http://localhost:5118 · http://localhost:5118/swagger |
| Mailbox (Mailpit) | http://localhost:8025 |
| SQL Server | `localhost,1433` — `sa` / `MSSQL_SA_PASSWORD`, database `EcommerceDb` |

Seeded logins: `admin` / `Admin@123`, and `customer1`–`customer3` / `Customer@123`.

### Day-to-day development

`docker-compose.yml` runs only the dependencies (SQL Server, Mailpit, and a built static frontend);
the API runs on the host. Stop the full stack first — they share host ports.

```bash
docker compose up -d sqlserver mailpit
dotnet ef database update --project ecommerce.Infrastructure --startup-project ecommerce.Api
./seeds/database/seed.sh
dotnet run --project ecommerce.Api
```

Frontend with hot reload:

```bash
cd ecommerce.Web && npm install && npm run dev
```

The API's CORS policy allows only `http://localhost:5173`, so the frontend has to stay on that port.

## Layout

Clean Architecture, four projects; dependencies point inward. `Application` declares the interfaces,
`Infrastructure` implements them.

| Project | Holds |
|---|---|
| `ecommerce.Domain` | Entities and enums. No EF, no MediatR. |
| `ecommerce.Application` | One folder per feature with `Commands/` + `Queries/`: handler, validator, DTO. Interfaces under `Common/Interfaces`. |
| `ecommerce.Infrastructure` | EF Core `ApplicationDbContext` + migrations, JWT, BCrypt hashing, local file storage, SMTP, queues and workers. |
| `ecommerce.Api` | Thin controllers that `ISender.Send` and return. |
| `ecommerce.Web` | React SPA — `src/api` (axios), `src/pages`, `src/components`, `src/store`. |

Conventions worth knowing before you edit:

- IDs are `Guid.CreateVersion7()`, assigned in `SaveChanges`; `CreatedAt`/`UpdatedAt` are stamped UTC there too.
- Products are soft-deleted behind a global query filter (`!IsDeleted`).
- Cart items store no price, so a cart always reflects the current product price; `OrderItem.PriceAtPurchased`
  freezes it at checkout.
- Every wallet movement is a `WalletTransaction` carrying the `OrderId` that caused it.

## API

All routes live under `/api/{controller}`; Swagger is on in Development.

| Area | Endpoints | Access |
|---|---|---|
| Auth | `POST login` · `POST register` · `POST logout` · `GET me` | public / signed in |
| Category | `GET` · `GET {id}` | public |
| | `POST` · `PUT {id}` · `DELETE {id}` | admin |
| Product | `GET` (paged, filterable) · `GET {id}` | public |
| | `POST` · `PUT {id}` · `PATCH {id}/price` · `DELETE {id}` | admin |
| Cart | `GET mycart` · `POST add` · `PATCH update/{itemId}` · `DELETE remove/{itemId}` · `DELETE clear` | customer |
| Checkout | `POST` | customer |
| Order | `GET` · `GET {id}` | customer |
| Wallet | `GET balance` · `GET transactions` | customer |
| Inventory | `POST job` → `202 { jobId }` · `GET jobs` | admin |

## Background work

Two in-memory `Channel<T>` queues, each drained by a `BackgroundService` with one DI scope per job:

- **Inventory upload** — the request stores the file and returns `202` with a job id; the worker parses it
  (CsvHelper for `.csv`, ClosedXML for `.xlsx`), updates product quantities and prices, and moves the file
  to `Storage/Archive` or `Storage/Failed`. Status moves `Stored → Accepted → Processing → Done | Failed`;
  the frontend polls `GET /api/inventory/jobs`.
- **Email** — checkout confirmations and upload reports; `EmailSentAt` on the order / job records delivery.

The queues are in-memory, so jobs still `Accepted` when the process restarts are dropped.

Upload files need the header `Quantity,Code,Price`. Samples, including the invalid ones, are in
[seeds/upload_test](seeds/upload_test).

## Configuration

| Key | Where |
|---|---|
| `ConnectionStrings__DefaultConnection` | env var in compose; user secrets or `appsettings.Development.json` locally |
| `JwtSettings` (`Key`, `Issuer`, `Audience`, `ExpiryMinutes`) | `appsettings.json` — the committed key is a dev placeholder, override it anywhere real |
| `Smtp` (`Host`, `Port`, `FromAddress`, …) | `appsettings.Development.json` points at Mailpit on `localhost:1025` |
| `Storage__RootPath` | `Storage`, holding `Uploads/`, `Archive/`, `Failed/` |
| `VITE_API_URL` | build arg for the frontend image; `.env` / Vite env locally |

## Seeds

```bash
./seeds/database/seed.sh          # base data: users, wallets, categories, products
./seeds/run-seed.sh               # menu of every seed in seeds/database
./seeds/run-seed.sh checkout      # a specific one
```

All seeds are idempotent — re-running them is safe.

## Docs

- [docs/system-design.md](docs/system-design.md) — architecture, layer rules, checkout and upload flows
- [docs/database-design.md](docs/database-design.md) — schema, indexes, delete rules
- [plan/](plan/README.md) — the feature-by-feature build plan this repo followed

## Not there yet

No automated tests ([plan/11-tests.md](plan/11-tests.md)) and no CI. The JWT signing key and the
permissive `AllowedHosts` in `appsettings.json` are development defaults, not production ones.
