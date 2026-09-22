using System.Text;
using System.Text.Json.Serialization;
using CampusRelay.Api.Data;
using CampusRelay.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

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
                      "- get one from POST /api/v1/auth/dev-login first.",
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

// SQLite for zero-setup local dev; Azure SQL Database for the deployed target
// (matches Part 1's "Development Stack & Cloud Infrastructure" section exactly).
builder.Services.AddDbContext<CampusRelayDbContext>(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.UseSqlite(
            builder.Configuration.GetConnectionString("DevSqlite") ?? "Data Source=campusrelay-dev.db");
    }
    else
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("AzureSql"));
    }
});

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// REQ-AUTH-1: the real deployment validates OIDC tokens from the university's SSO
// (Microsoft Azure AD). For the prototype, these same JWT settings both sign AND
// validate tokens issued by AuthController's dev-login endpoint - swap the
// Authority/Audience for Azure AD's values (and delete AuthController) once that
// integration exists.
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

// Dev-friendly CORS so the Android emulator (10.0.2.2) and Swagger UI can both call this
// without fuss. Tighten this to a real allow-list before anything is deployed publicly.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Auto-create the SQLite schema for local dev so there's nothing to run by hand.
    // For the real Azure SQL deployment, use EF Core migrations instead:
    //   dotnet ef migrations add InitialCreate
    //   dotnet ef database update
    // (see README.md - both need the .NET SDK's `dotnet-ef` tool, which this sandbox
    // doesn't have, so no Migrations/ folder is checked in yet).
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
