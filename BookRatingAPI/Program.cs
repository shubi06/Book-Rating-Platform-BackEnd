using System.Text;
using BookRatingAPI.Data;
using BookRatingAPI.Models;
using BookRatingAPI.Middleware;
using BookRatingAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Nest;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddMemoryCache();

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc(
        "v1",
        new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Book Rating API",
            Version = "v1",
            Description = "API for managing book ratings and reviews",
        }
    );

    // Configure JWT authentication in Swagger
    c.AddSecurityDefinition(
        "Bearer",
        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Description =
                "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
            Name = "Authorization",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
        }
    );

    c.AddSecurityRequirement(
        new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer",
                    },
                },
                Array.Empty<string>()
            },
        }
    );
});

// Configure database context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddSingleton<IElasticClient>(sp =>
{
    var config = builder.Configuration.GetSection("Elasticsearch");
    var uri = config["Uri"];
    var defaultIndex = config["DefaultIndex"];
    var username = config["Username"];
    var password = config["Password"];

    var settings = new ConnectionSettings(new Uri(uri))
        .DefaultIndex(defaultIndex)
        .DefaultMappingFor<Book>(m => m.IdProperty(p => p.Id))
        .BasicAuthentication(username, password)
        .ServerCertificateValidationCallback((o, cert, chain, errors) => true)
        .DisableDirectStreaming();

    return new ElasticClient(settings);
});

// Register application services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IRatingService, RatingService>();
builder.Services.AddScoped<IReadingListService, ReadingListService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IElasticSearchService, ElasticSearchService>();
builder.Services.AddScoped<IBookSyncService, BookSyncService>();
builder.Services.AddScoped<IRecommendationsService, RecommendationsService>();
builder.Services.AddScoped<IFollowService, FollowService>();

// Configure JWT authentication (stateless, token-based)
// SECURITY: Store JWT:Key in Azure Key Vault in production, not appsettings.json
builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
            ),
        };
    });

// Configure CORS for frontend access
// SECURITY: Only specify trusted origins, never use AllowAnyOrigin() with AllowCredentials()
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowFrontend",
        policy =>
        {
            policy
                .WithOrigins(
                    "http://localhost:3000",      // Local development
                    "http://localhost:5173",      // Vite default port
                    "http://localhost:4200",      // Angular default port
                    "http://51.124.72.116"        // Frontend production URL
                )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    );
});

// Add Health Checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Run database migrations
try
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
        Console.WriteLine("Database migrations applied successfully.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Warning: Failed to apply database migrations: {ex.Message}");
}

// Try to reindex Elasticsearch books, but don't crash if Elasticsearch is unavailable
try
{
    using (var scope = app.Services.CreateScope())
    {
        var elastic = scope.ServiceProvider.GetRequiredService<IElasticSearchService>();
        await elastic.ReindexAllBooks();
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Warning: Failed to reindex Elasticsearch books: {ex.Message}");
}

// Configure HTTP request pipeline
// IMPORTANT: Middleware order matters - exception handler must be first

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

// HTTPS redirection disabled - using HTTP Load Balancer in production
// app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

// Authentication must come before Authorization
app.UseAuthentication();  // Validates JWT token, sets HttpContext.User
app.UseAuthorization();   // Checks [Authorize] attributes and roles

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
