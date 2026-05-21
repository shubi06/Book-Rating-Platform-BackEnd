# Project Context for Claude Code

> Auto-loaded by Claude Code. Keep it accurate.

---

## What this project is

A RESTful Web API for a book rating platform where users can browse books, submit ratings/reviews, manage a personal reading list, and search books via Elasticsearch.

## Tech stack

- **Language:** C# 12 / .NET 8.0
- **Framework:** ASP.NET Core 8.0 Web API
- **Database:** SQL Server (via Entity Framework Core 8.0)
- **Search engine:** Elasticsearch 7.x via NEST 7.17.5
- **Auth:** JWT Bearer tokens (HMAC-SHA256, 7-day expiry)
- **Password hashing:** BCrypt.Net-Next (cost factor 10)
- **Caching:** In-memory (`IMemoryCache`, 5–10 min TTL)
- **API docs:** Swagger / Swashbuckle 6.4
- **Package manager:** NuGet (dotnet CLI)
- **SDK:** .NET 8.0 (pinned in `global.json`)

## Commands the agents will run

```bash
# Typecheck / build (run continuously)
dotnet build BookRatingAPI/BookRatingAPI.csproj

# Run the API locally
dotnet run --project BookRatingAPI/BookRatingAPI.csproj

# EF Core: add a migration
dotnet ef migrations add <MigrationName> --project BookRatingAPI

# EF Core: apply migrations manually
dotnet ef database update --project BookRatingAPI

# No test project exists yet
```

## Architecture

The codebase follows a layered architecture with dependency injection throughout.

### Layers
- **Controllers** — thin HTTP layer; delegates entirely to services
- **Services** — all business logic; each has an interface (`IXxxService`) and implementation (`XxxService`)
- **Data** — `AppDbContext` (EF Core), 5 `DbSet`s
- **Models** — EF entity classes (`User`, `Book`, `Category`, `Rating`, `ReadingList`)
- **DTOs** — one folder per domain; no entities leak out of services
- **Middleware** — `GlobalExceptionHandlerMiddleware` (first in pipeline), `RequestLoggingMiddleware`

### Service registrations (DI lifetime)
- All services: **Scoped**
- `IElasticClient`: **Singleton**

### Startup behavior
1. Auto-apply EF Core migrations (`MigrateAsync`) — warns but does not crash on failure
2. Full Elasticsearch reindex (`ReindexAllBooks`) — warns but does not crash on failure

## Domain models & key constraints

| Entity | Key fields | Unique indexes |
|---|---|---|
| `User` | Id, Username, Email, PasswordHash, IsAdmin | Email, Username |
| `Book` | Id, Title, Author, Description, CoverImageUrl, PublicationYear, ISBN, CategoryId | — |
| `Category` | Id, Name | — |
| `Rating` | Id, UserId, BookId, Score (1–5), Comment | (UserId, BookId) — one rating per user per book |
| `ReadingList` | Id, UserId, BookId, Status | (UserId, BookId) — one entry per user per book |

`ReadingStatus` enum: `WantToRead = 1`, `Read = 2`

## API surface

| Controller | Route prefix | Auth required | Notes |
|---|---|---|---|
| `AuthController` | `/api/auth` | None | `POST /register`, `POST /login` → returns JWT |
| `BooksController` | `/api/books` | GET: public; CUD: Admin role | SQL LIKE search via `?search=`, filter by `?categoryId=` |
| `RatingsController` | `/api/ratings` | Most: any auth; GET book ratings: public | One rating per user per book enforced in service |
| `ReadingListController` | `/api/readinglist` | All: any auth | Filter by `?status=` |
| `ProfileController` | `/api/profile` | GET own: auth; GET `/{userId}`: public | Public profile returns last 5 ratings |
| `ElasticSearchController` | `/api/elasticsearch` | None | Fuzzy search, top-rated, by category/year, rating filter, sort, reindex |
| Health check | `/health` | None | ASP.NET Core health checks |

JWT claims: `NameIdentifier` (userId), `Email`, `Name` (username), `Role` ("Admin" or "User")

## Elasticsearch behaviour

- Reindexed in full on every startup (drop index → recreate → bulk insert from SQL in 500-doc batches)
- `BookSyncService` keeps ES in sync after every book/rating create, update, or delete
- `BookDto` is the indexed document type; `AverageRating` and `RatingCount` are denormalised onto it
- Fuzzy search uses `Fuzziness.Auto` on `Title` and `Author`
- Sort options: `title`, `author`, `rating` (default), `year` (strings use `.keyword` sub-field)

## Caching

- `GetBooksAsync` (no search): cached under `"books:all"` or `"books:category:{id}"` for 5 min
- `GetBookByIdAsync`: cached under `"book:{id}"` for 10 min
- Cache is invalidated by `BookSyncService.SyncBookAsync` / `RemoveBookAsync` on every mutation

## Conventions

- Service interfaces live alongside their implementations: `Services/XxxService/IXxxService.cs` + `XxxService.cs`
- DTOs live in `DTOs/<Domain>DTOs/` (one file per DTO), except `ReadingListDtos.cs` which groups all three reading-list DTOs in one file
- Controllers extract `userId` from `ClaimTypes.NameIdentifier` — never from the request body
- Error responses are plain JSON objects with a `Message` field: `{ "Message": "..." }`
- Structured logging with `ILogger<T>` throughout; log `{PascalCase}` named params
- CORS: explicit origin allowlist only (`AllowFrontend` policy); never `AllowAnyOrigin`
- Exception handler runs first in the middleware pipeline; details field only populated in Development
- `Nullable` is enabled — use `?` types where nullability is real, not as a workaround

## Anti-patterns (do not do these)

- Do not put business logic in controllers — controllers are thin HTTP adapters only
- Do not expose EF entity classes in API responses — always map to DTOs
- Do not bypass `BookSyncService` when mutating books or ratings — ES and cache will go stale
- Do not add `AllowAnyOrigin()` to CORS — security requirement
- Do not store JWT key or DB passwords in source — use Azure Key Vault / environment variables in production
- Do not use `EF.Functions.Like` with unsanitized user input without parameterization (EF Core handles this, but be aware)
- Do not hardcode `"books"` index name outside `ElasticSearchService` — it's already duplicated there, don't spread it further

## Repository layout

```
Book-Rating-Platform/
├── BookRatingAPI/
│   ├── Controllers/        # HTTP layer — AuthController, BooksController, RatingsController,
│   │                       #   ReadingListController, ProfileController, ElasticSearchController
│   ├── Data/               # AppDbContext (EF Core)
│   ├── DTOs/               # Data transfer objects, grouped by domain
│   │   ├── AuthDTOs/
│   │   ├── BookDTOs/
│   │   ├── CategoryDTOs/
│   │   ├── ProfileDTOs/
│   │   ├── RatingDTOs/
│   │   └── ReadingListDtos.cs
│   ├── Middleware/          # GlobalExceptionHandlerMiddleware, RequestLoggingMiddleware
│   ├── Migrations/          # EF Core migrations (auto-applied on startup)
│   ├── Models/              # EF entity classes + ReadingStatus enum
│   ├── Services/            # Business logic — one subfolder per service with interface + impl
│   │   ├── AuthService/
│   │   ├── BookService/
│   │   ├── BookSyncService/ # Keeps ES + cache in sync after mutations
│   │   ├── CategoryService/
│   │   ├── ElasticSearchService/
│   │   ├── ProfileService/
│   │   ├── RatingService/
│   │   ├── ReadingListService/
│   │   └── TokenService/    # JWT generation
│   ├── appsettings.json     # Connection strings, ES config, JWT config, log levels
│   ├── global.json          # SDK version pin (8.0.0)
│   └── Program.cs           # DI registration, middleware pipeline, startup tasks
└── CLAUDE.md
```

## Where Boris workflow artifacts live

- `docs/research.md` — written by the `researcher` agent
- `docs/plan.md` — written by the `planner`, annotated by the `annotator`, updated by `plan-updater`, finalized by `todo-builder`
- `docs/followups.md` — out-of-scope issues the `implementer` noticed (appended over time)

Treat all three as project artifacts. Commit them with the feature.

## Operating rules for Claude

1. Never write code during research or planning. The implementation phase is the only one that touches source files.
2. The plan is the source of truth during implementation. Tick off tasks as you go.
3. Nullable is enabled — no nullable warnings allowed. Run `dotnet build` continuously.
4. No comments unless the WHY is non-obvious. No XML doc comments on obvious CRUD.
5. If something is impossible or wrong, STOP and tell the user. Don't silently deviate.
6. When stuck, suggest revert-and-re-scope rather than patching incrementally.
