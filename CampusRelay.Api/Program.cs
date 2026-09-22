using System;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;
using CampusRelay.Api.Data;
using CampusRelay.Api.Services;
using FirebaseAdmin;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

// CampusRelay API - PROG7314 POE Project
// Developed by: James (semaj-007)
// Institution: VC Connect / University
// Date: 2024
// Purpose: Backend API for CampusRelay mobile application
// Features: Firebase Authentication, PostgreSQL/Render deployment, JWT token management

var builder = WebApplication.CreateBuilder(args);

// Set the base directory for Firebase service account file lookup
FirebaseAppContext.BaseDirectory = FirebaseAppContext.BaseDirectory ?? AppDomain.CurrentDomain.BaseDirectory;

// REQ-API-2: every response is JSON; enums serialize as their name (e.g. "Active")
// rather than a raw integer, so payloads stay readable in Swagger/Postman/logcat.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "CampusRelay API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\" " +
                      "- get one from POST /api/v1/auth/dev-login for development.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// Database configuration: SQLite for dev, PostgreSQL for Render, Azure SQL for other deployments
builder.Services.AddDbContext<CampusRelayDbContext>(options =>
{
    var env = builder.Environment.EnvironmentName;
    
    if (env == "Development")
    {
        // Local development with SQLite
        options.UseSqlite(
            builder.Configuration.GetConnectionString("DevSqlite") ?? "Data Source=campusrelay-dev.db");
    }
    else if (env == "Production" || Environment.GetEnvironmentVariable("RENDER") != null)
    {
        // Render deployment with PostgreSQL
        var connectionString = builder.Configuration.GetConnectionString("RenderPostgres") 
            ?? Environment.GetEnvironmentVariable("DATABASE_URL");
        
        if (!string.IsNullOrEmpty(connectionString))
        {
            // Render provides DATABASE_URL in PostgreSQL format
            // Convert from PostgreSQL URI format to connection string if needed
            if (connectionString.StartsWith("postgres://"))
            {
                connectionString = ConvertPostgresUriToConnectionString(connectionString);
            }
            options.UseNpgsql(connectionString);
        }
    }
    else
    {
        // Azure SQL fallback
        options.UseSqlServer(builder.Configuration.GetConnectionString("AzureSql"));
    }
});

// Helper method to convert Render's PostgreSQL URI to connection string
static string ConvertPostgresUriToConnectionString(string postgresUri)
{
    // Format: postgres://username:password@host:port/database
    var uri = new Uri(postgresUri);
    var userInfo = uri.UserInfo.Split(':');
    var host = uri.Host;
    var port = uri.Port;
    var database = uri.LocalPath.TrimStart('/');
    
    return $"Host={host};Port={port};Database={database};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
}

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// Firebase Authentication setup - reads from environment variable on Render
builder.Services.AddSingleton<IFirebaseTokenValidator, FirebaseTokenValidator>();

// JWT Bearer Authentication for API endpoints
var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!))
        };
    });
builder.Services.AddAuthorization();

// CORS configuration for Render - allow Android emulator and any frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

// Configure for Render's PORT environment variable
if (Environment.GetEnvironmentVariable("PORT") != null)
{
    app.Urls.Add("http://*:" + Environment.GetEnvironmentVariable("PORT"));
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Auto-create the SQLite schema for local dev so there's nothing to run by hand.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CampusRelayDbContext>();
    db.Database.EnsureCreated();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
