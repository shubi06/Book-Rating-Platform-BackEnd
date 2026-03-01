# Dokumentimi i Projektit: Book Rating API

## 1. Përmbledhje Ekzekutive

Book Rating API është një platformë e plotë për menaxhimin e librave, vlerësimeve dhe listave të leximit. Projekti është zhvilluar si pjesë e programit LIFE 3 / Projekti 8 dhe ofron një API RESTful të ndërtuar me ASP.NET Core 8.0.

### Qëllimi i Projektit
Të ofrojë një platformë moderne dhe të shkallëzueshme ku përdoruesit mund të:
- Zbulojnë dhe kërkojnë libra
- Vlerësojnë dhe komentojnë për libra
- Menaxhojnë listat e tyre të leximit
- Ndjekin progresin e leximit
- Shikojnë profile publike të përdoruesve të tjerë

## 2. Arkitektura e Sistemit

### 2.1 Stack Teknologjik

**Backend Framework:**
- ASP.NET Core 8.0 (Web API)
- Entity Framework Core 8.0.8 (ORM)
- C# me Nullable Reference Types të aktivizuar

**Database:**
- SQL Server (Production & Development)
- Entity Framework Core Migrations për menaxhimin e skemës

**Kërkimi:**
- Elasticsearch 7.17.5 (NEST client)
- Indeksim automatik i librave

**Autentifikimi:**
- JWT (JSON Web Tokens)
- BCrypt.Net-Next 4.0.3 për hash-imin e fjalëkalimeve

**Dokumentimi i API:**
- Swagger/OpenAPI 3.0
- Swashbuckle.AspNetCore 6.4.0

**Deployment:**
- Azure Kubernetes Service (AKS)
- Docker containerization
- Azure SQL Database

### 2.2 Struktura e Projektit

```
BookRatingAPI/
├── Controllers/          # API endpoints
│   ├── AuthController.cs
│   ├── BooksController.cs
│   ├── CategoriesController.cs
│   ├── ElasticSearchController.cs
│   ├── ProfileController.cs
│   ├── RatingController.cs
│   └── ReadingListController.cs
├── Models/              # Domain entities
│   ├── User.cs
│   ├── Book.cs
│   ├── Rating.cs
│   ├── ReadingList.cs
│   ├── Category.cs
│   └── Enums/
│       └── ReadingStatus.cs
├── DTOs/                # Data Transfer Objects
│   ├── AuthDTOs/
│   ├── BookDTOs/
│   ├── CategoryDTOs/
│   ├── ProfileDTOs/
│   ├── RatingDTOs/
│   └── ReadingListDtos.cs
├── Services/            # Business logic
│   ├── AuthService/
│   ├── BookService/
│   ├── BookSyncService/
│   ├── CategoryService/
│   ├── ElasticSearchService/
│   ├── ProfileService/
│   ├── RatingService/
│   ├── ReadingListService/
│   └── TokenService/
├── Data/                # Database context
│   └── AppDbContext.cs
├── Middleware/          # Custom middleware
│   ├── GlobalExceptionHandlerMiddleware.cs
│   └── RequestLoggingMiddleware.cs
└── Migrations/          # EF Core migrations
```

## 3. Modelet e të Dhënave

### 3.1 User (Përdoruesi)
```csharp
- Id: int (Primary Key)
- Username: string (max 100 karaktere, i detyrueshëm)
- Email: string (EmailAddress, i detyrueshëm)
- PasswordHash: string (BCrypt hash, i detyrueshëm)
- IsAdmin: bool (default: false)
- CreatedAt: DateTime (UTC)
- Ratings: ICollection<Rating> (Navigation property)
- ReadingLists: ICollection<ReadingList> (Navigation property)
```

**Karakteristikat e Sigurisë:**
- Fjalëkalimet hash-ohen me BCrypt para ruajtjes
- Format i hash: $2a$10$[22 chars salt][31 chars hash]
- Asnjëherë nuk ruhen fjalëkalime në tekst të thjeshtë
- Role-based access control (RBAC) me IsAdmin flag

### 3.2 Book (Libri)
```csharp
- Id: int (Primary Key)
- Title: string (max 200 karaktere, i detyrueshëm)
- Author: string (max 100 karaktere, i detyrueshëm)
- Description: string
- CoverImageUrl: string? (nullable)
- PublicationYear: int? (nullable)
- ISBN: string? (nullable)
- CategoryId: int (Foreign Key)
- Category: Category (Navigation property)
- CreatedAt: DateTime (UTC)
- Ratings: ICollection<Rating> (Navigation property)
- ReadingLists: ICollection<ReadingList> (Navigation property)
```

**Karakteristikat:**
- Indeksohet në Elasticsearch për kërkim të shpejtë
- Mbështet imazhe të kopertinave
- Lidhje me kategori
- Metadata e plotë (ISBN, viti i publikimit)

### 3.3 Rating (Vlerësimi)
```csharp
- Id: int (Primary Key)
- UserId: int (Foreign Key)
- User: User (Navigation property)
- BookId: int (Foreign Key)
- Book: Book (Navigation property)
- Score: int (Range: 1-5, i detyrueshëm)
- Comment: string? (max 1000 karaktere, nullable)
- CreatedAt: DateTime (UTC)
- UpdatedAt: DateTime? (nullable)
```

**Rregullat e Biznesit:**
- Një përdorues mund të vlerësojë një libër vetëm një herë
- Vlerësimi duhet të jetë midis 1 dhe 5
- Komentet janë opsionale por të kufizuara në 1000 karaktere
- Mbështet përditësime (UpdatedAt timestamp)

### 3.4 ReadingList (Lista e Leximit)
```csharp
- Id: int (Primary Key)
- UserId: int (Foreign Key)
- User: User (Navigation property)
- BookId: int (Foreign Key)
- Book: Book (Navigation property)
- Status: ReadingStatus (enum)
- AddedAt: DateTime (UTC)
```

**Statuset e Leximit:**
```csharp
public enum ReadingStatus
{
    WantToRead,    // Dëshiron të lexojë
    Reading,       // Duke lexuar
    Completed      // E përfunduar
}
```

### 3.5 Category (Kategoria)
```csharp
- Id: int (Primary Key)
- Name: string (max 50 karaktere, i detyrueshëm)
- Books: ICollection<Book> (Navigation property)
```

## 4. API Endpoints

### 4.1 Authentication (AuthController)

**POST /api/auth/register**
- Regjistron një përdorues të ri
- Body: `{ username, email, password }`
- Response: `{ token, user: { id, username, email, isAdmin } }`
- Validime: Email unik, username unik, fjalëkalim i fortë

**POST /api/auth/login**
- Autentifikon një përdorues ekzistues
- Body: `{ email, password }`
- Response: `{ token, user: { id, username, email, isAdmin } }`
- Gjeneron JWT token me kohëzgjatje të konfiguruar

### 4.2 Books (BooksController)

**GET /api/books**
- Merr listën e të gjithë librave
- Query params: pagination, filtering
- Response: Array i BookDto
- Public endpoint (nuk kërkon autentifikim)

**GET /api/books/{id}**
- Merr detajet e një libri specifik
- Response: BookDto me të gjitha detajet
- Përfshin vlerësimin mesatar dhe numrin e vlerësimeve

**POST /api/books**
- Krijon një libër të ri
- Requires: [Authorize(Roles = "Admin")]
- Body: CreateBookDto
- Response: BookDto i krijuar

**PUT /api/books/{id}**
- Përditëson një libër ekzistues
- Requires: [Authorize(Roles = "Admin")]
- Body: CreateBookDto
- Response: BookDto i përditësuar

**DELETE /api/books/{id}**
- Fshin një libër
- Requires: [Authorize(Roles = "Admin")]
- Response: 204 No Content

### 4.3 Categories (CategoriesController)

**GET /api/categories**
- Merr të gjitha kategoritë
- Response: Array i CategoryDto
- Public endpoint

**POST /api/categories**
- Krijon një kategori të re
- Requires: [Authorize(Roles = "Admin")]
- Body: CreateCategoryDto
- Response: CategoryDto

### 4.4 Ratings (RatingController)

**GET /api/ratings/book/{bookId}**
- Merr të gjitha vlerësimet për një libër
- Response: Array i RatingDto
- Public endpoint

**POST /api/ratings**
- Krijon një vlerësim të ri
- Requires: [Authorize]
- Body: CreateRatingDto `{ bookId, score, comment? }`
- Response: RatingDto
- Validim: Një përdorues mund të vlerësojë një libër vetëm një herë

**PUT /api/ratings/{id}**
- Përditëson një vlerësim ekzistues
- Requires: [Authorize] + ownership check
- Body: CreateRatingDto
- Response: RatingDto i përditësuar

**DELETE /api/ratings/{id}**
- Fshin një vlerësim
- Requires: [Authorize] + ownership check
- Response: 204 No Content

### 4.5 Reading List (ReadingListController)

**GET /api/readinglist**
- Merr listën e leximit të përdoruesit të autentifikuar
- Requires: [Authorize]
- Query params: status filter (optional)
- Response: Array i ReadingListDto

**POST /api/readinglist**
- Shton një libër në listën e leximit
- Requires: [Authorize]
- Body: `{ bookId, status }`
- Response: ReadingListDto

**PUT /api/readinglist/{id}**
- Përditëson statusin e një libri në listë
- Requires: [Authorize] + ownership check
- Body: `{ status }`
- Response: ReadingListDto i përditësuar

**DELETE /api/readinglist/{id}**
- Heq një libër nga lista e leximit
- Requires: [Authorize] + ownership check
- Response: 204 No Content

### 4.6 Profile (ProfileController)

**GET /api/profile**
- Merr profilin e përdoruesit të autentifikuar
- Requires: [Authorize]
- Response: ProfileDto me statistika dhe vlerësime të fundit

**GET /api/profile/{userId}**
- Merr profilin publik të një përdoruesi
- Response: PublicProfileDto
- Public endpoint

**Statistikat e Profilit:**
- Numri total i librave të lexuar
- Numri total i vlerësimeve
- Vlerësimi mesatar i dhënë
- Vlerësimet e fundit (5 të fundit)
- Librat në progres

### 4.7 Elasticsearch (ElasticSearchController)

**GET /api/elasticsearch/search**
- Kërkon libra në Elasticsearch
- Query params: `query` (search term)
- Response: Array i BookDto
- Kërkon në: Title, Author, Description
- Public endpoint

**POST /api/elasticsearch/reindex**
- Ri-indekson të gjithë librat në Elasticsearch
- Requires: [Authorize(Roles = "Admin")]
- Response: Success message
- Përdoret për mirëmbajtje dhe sinkronizim

## 5. Autentifikimi dhe Autorizimi

### 5.1 JWT Configuration

**Token Structure:**
```json
{
  "sub": "user_id",
  "email": "user@example.com",
  "role": "Admin" | "User",
  "iss": "BookRatingAPI",
  "aud": "BookRatingAPI",
  "exp": "expiration_timestamp",
  "iat": "issued_at_timestamp"
}
```

**Token Validation:**
- ValidateIssuer: true
- ValidateAudience: true
- ValidateLifetime: true
- ValidateIssuerSigningKey: true
- Signing Algorithm: HS256 (HMAC-SHA256)

**Security Best Practices:**
- JWT:Key duhet të ruhet në Azure Key Vault në production
- Tokens kanë kohëzgjatje të kufizuar (e konfiguruar në appsettings)
- Stateless authentication (nuk ruhen sessions në server)

### 5.2 Authorization Levels

**Public Endpoints:**
- GET /api/books
- GET /api/books/{id}
- GET /api/categories
- GET /api/ratings/book/{bookId}
- GET /api/profile/{userId}
- GET /api/elasticsearch/search

**Authenticated Endpoints ([Authorize]):**
- POST /api/ratings
- PUT /api/ratings/{id}
- DELETE /api/ratings/{id}
- GET /api/readinglist
- POST /api/readinglist
- PUT /api/readinglist/{id}
- DELETE /api/readinglist/{id}
- GET /api/profile

**Admin Only ([Authorize(Roles = "Admin")]):**
- POST /api/books
- PUT /api/books/{id}
- DELETE /api/books/{id}
- POST /api/categories
- POST /api/elasticsearch/reindex

## 6. Middleware dhe Error Handling

### 6.1 GlobalExceptionHandlerMiddleware

**Qëllimi:**
- Kap të gjitha exception-et e pa-kapur
- Kthen përgjigje të standardizuara JSON
- Log-on errors për debugging
- Fsheh detaje të brendshme në production

**Response Format:**
```json
{
  "error": "Error message",
  "statusCode": 500,
  "timestamp": "2026-03-01T12:00:00Z"
}
```

### 6.2 RequestLoggingMiddleware

**Qëllimi:**
- Log-on të gjitha HTTP requests
- Regjistron: Method, Path, Status Code, Duration
- Ndihmon në monitoring dhe debugging
- Performance tracking

**Log Format:**
```
[INFO] HTTP GET /api/books - 200 OK (125ms)
```

## 7. Database dhe Migrations

### 7.1 Connection String

**Development:**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=BookRatingDB;..."
}
```

**Production (Azure):**
- Ruhet në Azure Key Vault
- Injektohet si environment variable në Kubernetes
- Connection pooling i aktivizuar

### 7.2 Migrations

**Migrations Ekzistuese:**
1. `20260215224425_initialCreate` - Krijimi fillestar i skemës
2. `20260221203318_InitialCreateUpdate` - Përditësime të skemës

**Migration Strategy:**
- Automatic migrations në startup (Program.cs)
- `await dbContext.Database.MigrateAsync()`
- Graceful degradation nëse migrations dështojnë

### 7.3 Database Relationships

**One-to-Many:**
- User → Ratings (1:N)
- User → ReadingLists (1:N)
- Book → Ratings (1:N)
- Book → ReadingLists (1:N)
- Category → Books (1:N)

**Constraints:**
- Cascade delete për ratings dhe reading lists kur fshihet user
- Restrict delete për books kur ka ratings ekzistuese
- Unique constraints për User.Email dhe User.Username

## 8. Elasticsearch Integration

### 8.1 Configuration

**Connection Settings:**
```json
"Elasticsearch": {
  "Uri": "https://elasticsearch-url:9200",
  "DefaultIndex": "books",
  "Username": "elastic",
  "Password": "***"
}
```

**Security:**
- Basic Authentication
- SSL/TLS me certificate validation disabled (development)
- Credentials në Azure Key Vault (production)

### 8.2 Indexing Strategy

**Automatic Reindexing:**
- Në startup të aplikacionit
- Graceful degradation nëse Elasticsearch nuk është i disponueshëm
- Manual reindex endpoint për admins

**Indexed Fields:**
- Title (analyzed, searchable)
- Author (analyzed, searchable)
- Description (analyzed, searchable)
- ISBN (keyword, exact match)
- CategoryId (keyword)

**Search Features:**
- Full-text search
- Multi-field search (title, author, description)
- Fuzzy matching
- Relevance scoring

## 9. CORS Configuration

### 9.1 Allowed Origins

**Development:**
- http://localhost:3000 (React default)
- http://localhost:5173 (Vite default)
- http://localhost:4200 (Angular default)

**Production:**
- http://51.124.72.116 (Frontend production URL)

### 9.2 CORS Policy

```csharp
policy
    .WithOrigins(...)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials();
```

**Security Note:**
- Asnjëherë mos përdor `AllowAnyOrigin()` me `AllowCredentials()`
- Specifiko vetëm origins të besuar
- Përditëso origins kur deploy-on në mjedise të reja

## 10. Deployment dhe Infrastructure

### 10.1 Azure Kubernetes Service (AKS)

**Kubernetes Manifests:**
- `namespace.yaml` - Namespace për izolim
- `deployment.yaml` - API deployment configuration
- `service.yaml` - LoadBalancer service
- `sql-server.yaml` - SQL Server deployment
- `elasticsearch.yaml` - Elasticsearch deployment
- `secrets-example.yaml` - Template për secrets
- `migration-job.yaml` - Database migration job

**Deployment Strategy:**
- Rolling updates
- Health checks në `/health` endpoint
- Readiness dhe liveness probes
- Resource limits dhe requests

### 10.2 Docker Configuration

**Dockerfile Characteristics:**
- Multi-stage build për optimizim
- Base image: mcr.microsoft.com/dotnet/aspnet:8.0
- SDK image: mcr.microsoft.com/dotnet/sdk:8.0
- Port exposure: 8080 (HTTP)

### 10.3 Health Checks

**Endpoint:** `/health`
- Kontrollon database connectivity
- Kontrollon Elasticsearch availability
- Response: 200 OK nëse healthy, 503 Service Unavailable nëse jo

## 11. Siguria

### 11.1 Best Practices të Implementuara

**Password Security:**
- BCrypt hashing me salt automatik
- Work factor: 10 (2^10 = 1024 iterations)
- Asnjë fjalëkalim në plaintext

**JWT Security:**
- Signing key në Key Vault
- Token expiration
- HTTPS only në production (përmes load balancer)

**Input Validation:**
- Data Annotations në DTOs
- Model validation në controllers
- SQL Injection prevention (EF Core parameterized queries)

**Authorization:**
- Role-based access control
- Ownership checks për resources
- Principle of least privilege

### 11.2 Security Headers

**Recommendations për Production:**
- X-Content-Type-Options: nosniff
- X-Frame-Options: DENY
- X-XSS-Protection: 1; mode=block
- Strict-Transport-Security (HSTS)
- Content-Security-Policy

## 12. Monitoring dhe Logging

### 12.1 Logging Strategy

**Current Implementation:**
- Console logging (stdout)
- Request/Response logging via middleware
- Exception logging në GlobalExceptionHandlerMiddleware

**Recommendations:**
- Structured logging me Serilog
- Log aggregation (Azure Application Insights)
- Log levels: Debug, Info, Warning, Error, Critical
- Correlation IDs për request tracking

### 12.2 Metrics

**Key Metrics to Monitor:**
- Request rate dhe latency
- Error rate (4xx, 5xx)
- Database query performance
- Elasticsearch query performance
- Memory dhe CPU usage
- Active connections

## 13. Testing Strategy

### 13.1 Recommended Test Types

**Unit Tests:**
- Service layer logic
- Business rules validation
- DTO mappings
- Utility functions

**Integration Tests:**
- API endpoints
- Database operations
- Elasticsearch integration
- Authentication flows

**End-to-End Tests:**
- Complete user workflows
- Multi-step operations
- Error scenarios

### 13.2 Test Coverage Goals

- Service layer: 80%+
- Controllers: 70%+
- Critical paths: 100%
- Edge cases dhe error handling

## 14. Performance Optimization

### 14.1 Current Optimizations

**Caching:**
- Memory cache për frequently accessed data
- `builder.Services.AddMemoryCache()`

**Database:**
- EF Core query optimization
- Eager loading për related entities
- Connection pooling

**Elasticsearch:**
- Async operations
- Bulk indexing për reindex
- DisableDirectStreaming për performance

### 14.2 Future Optimizations

**Caching Strategy:**
- Redis për distributed caching
- Cache invalidation policies
- Cache warming strategies

**Database:**
- Read replicas për scaling
- Query optimization dhe indexing
- Pagination për large datasets

**API:**
- Response compression
- API rate limiting
- CDN për static assets

## 15. API Versioning

### 15.1 Current Version

- Version: v1
- Swagger endpoint: `/swagger/v1/swagger.json`
- Base path: `/api/`

### 15.2 Versioning Strategy

**Recommendations:**
- URL versioning: `/api/v1/`, `/api/v2/`
- Header versioning: `Accept: application/vnd.bookrating.v1+json`
- Deprecation policy: 6 months notice
- Backward compatibility për minor versions

## 16. Documentation dhe API Discovery

### 16.1 Swagger/OpenAPI

**Features:**
- Interactive API documentation
- Try-it-out functionality
- JWT authentication support
- Schema definitions
- Example requests/responses

**Access:**
- Development: `http://localhost:5000/swagger`
- Production: `http://api-url/swagger`

### 16.2 Code Documentation

**Standards:**
- XML documentation comments për public APIs
- Inline comments për complex logic
- README files në çdo modul
- Architecture Decision Records (ADRs)

**Existing Documentation:**
- `COMMENTING_BEST_PRACTICES.md` - Udhëzime për komentim

## 17. Environment Configuration

### 17.1 Configuration Files

**appsettings.json:**
- Default configuration
- Shared settings për të gjitha mjediset

**appsettings.Development.json:**
- Development-specific overrides
- Verbose logging
- Local connection strings

**appsettings.Production.json:**
- Production-specific settings
- Minimal logging
- Azure-specific configuration

### 17.2 Secrets Management

**Development:**
- User secrets (dotnet user-secrets)
- Local environment variables

**Production:**
- Azure Key Vault
- Kubernetes secrets
- Environment variables në containers

## 18. Dependencies dhe Package Management

### 18.1 NuGet Packages

**Core Dependencies:**
- Microsoft.AspNetCore.OpenApi (8.0.8)
- Microsoft.EntityFrameworkCore (8.0.8)
- Microsoft.EntityFrameworkCore.SqlServer (8.0.8)
- Microsoft.EntityFrameworkCore.Tools (8.0.8)

**Authentication:**
- Microsoft.AspNetCore.Authentication.JwtBearer (8.0.8)
- BCrypt.Net-Next (4.0.3)

**Search:**
- NEST (7.17.5) - Elasticsearch client

**Documentation:**
- Swashbuckle.AspNetCore (6.4.0)

### 18.2 Update Strategy

- Quarterly dependency updates
- Security patches immediately
- Major version updates me testing të plotë
- Compatibility testing para production deployment

## 19. Development Workflow

### 19.1 Local Development Setup

**Prerequisites:**
- .NET 8.0 SDK
- SQL Server (LocalDB ose Docker)
- Elasticsearch (optional, për search features)
- IDE: Visual Studio, VS Code, ose Rider

**Setup Steps:**
1. Clone repository
2. Restore NuGet packages: `dotnet restore`
3. Update connection string në appsettings.Development.json
4. Run migrations: `dotnet ef database update`
5. Run application: `dotnet run`
6. Access Swagger: `http://localhost:5000/swagger`

### 19.2 Git Workflow

**Branching Strategy:**
- `main` - Production-ready code
- `develop` - Integration branch
- `feature/*` - Feature branches
- `bugfix/*` - Bug fix branches
- `hotfix/*` - Production hotfixes

**Commit Conventions:**
- Conventional Commits format
- Clear, descriptive messages
- Reference issue numbers

## 20. Future Enhancements

### 20.1 Planned Features

**User Features:**
- Social features (follow users, activity feed)
- Book recommendations based on ratings
- Reading goals dhe challenges
- Book clubs dhe discussions

**Admin Features:**
- Analytics dashboard
- User management
- Content moderation tools
- Bulk import/export

**Technical Improvements:**
- GraphQL API
- Real-time notifications (SignalR)
- Advanced search filters
- Image upload për book covers
- API rate limiting
- Request throttling

### 20.2 Scalability Considerations

**Horizontal Scaling:**
- Stateless API design
- Load balancing në Kubernetes
- Database read replicas
- Distributed caching

**Vertical Scaling:**
- Resource optimization
- Query performance tuning
- Connection pool sizing
- Memory management

## 21. Compliance dhe Legal

### 21.1 Data Privacy

**GDPR Considerations:**
- User data export functionality
- Right to be forgotten (account deletion)
- Data retention policies
- Privacy policy dhe terms of service

### 21.2 Content Moderation

**User-Generated Content:**
- Rating comments moderation
- Inappropriate content reporting
- Admin review tools
- Automated content filtering

## 22. Support dhe Maintenance

### 22.1 Monitoring

**Application Monitoring:**
- Health check endpoint
- Error tracking
- Performance metrics
- User analytics

**Infrastructure Monitoring:**
- Kubernetes cluster health
- Database performance
- Elasticsearch cluster status
- Resource utilization

### 22.2 Backup dhe Recovery

**Database Backups:**
- Automated daily backups
- Point-in-time recovery
- Backup retention: 30 days
- Disaster recovery plan

**Elasticsearch Backups:**
- Snapshot repository
- Automated snapshots
- Index recovery procedures

## 23. Team dhe Contacts

### 23.1 Project Information

**Program:** LIFE 3 Program
**Project Number:** Project 8
**Project Name:** Book Rating Platform

### 23.2 Documentation Maintenance

**Last Updated:** March 1, 2026
**Version:** 1.0
**Maintained By:** Development Team

**Update Schedule:**
- Major updates: Quarterly
- Minor updates: As needed
- Security updates: Immediately

---

## Appendix A: Glossary

**API:** Application Programming Interface
**DTO:** Data Transfer Object
**EF Core:** Entity Framework Core
**JWT:** JSON Web Token
**ORM:** Object-Relational Mapping
**RBAC:** Role-Based Access Control
**REST:** Representational State Transfer
**SQL:** Structured Query Language
**UTC:** Coordinated Universal Time

## Appendix B: References

- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [Entity Framework Core Documentation](https://docs.microsoft.com/ef/core)
- [Elasticsearch Documentation](https://www.elastic.co/guide)
- [JWT Best Practices](https://tools.ietf.org/html/rfc8725)
- [Azure Kubernetes Service Documentation](https://docs.microsoft.com/azure/aks)

## Appendix C: Change Log

### Version 1.0 (March 1, 2026)
- Initial comprehensive documentation
- Complete API reference
- Architecture overview
- Deployment guide
- Security best practices
