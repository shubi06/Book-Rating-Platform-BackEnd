# BookRatingAPI.Tests

Unit tests për Book Rating Platform backend.

## Code Coverage

**Current Coverage: 23.1%** ✅ (Target: 20%)

- Line Coverage: 23.1% (536/2311 lines)
- Branch Coverage: 22.9% (55/240 branches)
- Method Coverage: 58.8% (130/221 methods)

## Test Structure

### Services Tests (100% Coverage)
- ✅ **AuthService** - Authentication dhe registration
- ✅ **TokenService** - JWT token generation
- ✅ **CategoryService** - Category CRUD operations
- ✅ **RatingService** - Book ratings management
- ✅ **ReadingListService** - Reading list management

### Total Tests: 32
- AuthServiceTests: 6 tests
- TokenServiceTests: 2 tests
- CategoryServiceTests: 7 tests
- RatingServiceTests: 9 tests
- ReadingListServiceTests: 8 tests

## Running Tests

### Run all tests
```bash
dotnet test
```

### Run tests with code coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Generate HTML coverage report
```bash
reportgenerator -reports:"TestResults/*/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
```

Pastaj hap `coveragereport/index.html` në browser.

## Dependencies

- xUnit - Test framework
- Moq - Mocking library
- Microsoft.EntityFrameworkCore.InMemory - In-memory database për testing
- coverlet.collector - Code coverage collector
- ReportGenerator - Coverage report generator

## Test Patterns

### In-Memory Database
Çdo test përdor një in-memory database të veçantë për të shmangur ndërhyrjet:

```csharp
private AppDbContext GetInMemoryContext()
{
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;
    return new AppDbContext(options);
}
```

### Mocking Dependencies
Dependencies si ILogger dhe IBookSyncService janë mock-uar duke përdorur Moq:

```csharp
var logger = new Mock<ILogger<CategoryService>>();
var service = new CategoryService(context, logger.Object);
```

## CI/CD Integration

Tests ekzekutohen automatikisht në GitHub Actions për çdo push dhe pull request. Shiko `.github/workflows/ci-cd.yml` për detaje.
