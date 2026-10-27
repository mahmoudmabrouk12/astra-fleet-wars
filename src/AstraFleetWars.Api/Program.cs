using System.Text;
using System.Text.Json.Serialization;
using AstraFleetWars.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Controllers with JSON camelCase formatting
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// 2. Register Application Services (DI)
builder.Services.AddSingleton<IPlayerRepository, InMemoryPlayerRepository>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<ISyncService, SyncService>();

// 3. Configure JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "astra-syndicate-fleet-wars-super-secret-key-2024-min-32-chars";
var key = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
});

// 4. Configure Swagger / OpenAPI Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Astra Syndicate: Fleet Wars API (.NET 9)",
        Version = "v1.0.0",
        Description = "Sprint 1 Technical Deliverable (BE-01, BE-02, BE-03) for Offline-to-Online Synchronization and Player Metadata Architecture.",
        Contact = new OpenApiContact
        {
            Name = "Mahmoud Mabrouk - Backend Engineer",
            Email = "mahmoud@mkcreative.agency"
        }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// 5. Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure Middleware Pipeline
app.UseCors("AllowAll");

// Mount Swagger at both /swagger and /api-docs for convenience
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Astra Fleet Wars API v1");
    c.RoutePrefix = "api-docs"; // Accessible at /api-docs
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

Console.WriteLine("=======================================================");
Console.WriteLine("🚀 Astra Syndicate: Fleet Wars Backend (.NET 9)");
Console.WriteLine("📡 Sprint 1 Deliverable Active (ASP.NET Core Web API)");
Console.WriteLine("📑 OpenAPI / Swagger UI: http://localhost:5000/api-docs");
Console.WriteLine("⚡ Supabase Mode: LOCAL MOCK (Ready)");
Console.WriteLine("=======================================================");

app.Run();
