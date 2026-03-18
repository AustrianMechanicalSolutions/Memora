using System.Text;
using AuthApi.Data;
using AuthApi.Models;
using AuthApi.Services;
using AuthApi.Endpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// ---- JWT config ----
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection("Jwt"));

// ---- EF Core (SQLite) ----
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---- Security services ----
builder.Services.AddSingleton<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

// ---- Auth ----
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey =
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Query["token"];

                if (!string.IsNullOrEmpty(token))
                {
                    context.Token = token;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ---- CORS (Angular) ----
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200", "https://austrianms.at", "https://www.austrianms.at", "https://memora.austrianms.at")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();
var webRootPath = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "uploads");

Directory.CreateDirectory(uploadsPath);

app.UseHttpsRedirection();
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();

// ---- Ensure interaction tables exist (SQLite, no migrations) ----
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS MemoryLikes (
    MemoryId TEXT NOT NULL,
    UserId TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    PRIMARY KEY (MemoryId, UserId)
);");
    db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS MemoryComments (
    Id TEXT NOT NULL PRIMARY KEY,
    MemoryId TEXT NOT NULL,
    UserId TEXT NOT NULL,
    Content TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    ParentCommentId TEXT NULL
);");
    db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS CommentLikes (
    CommentId TEXT NOT NULL,
    UserId TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    PRIMARY KEY (CommentId, UserId)
);");
}

// ---- Map endpoints ----
app.MapAuthEndpoints();

app.MapControllers();

// ---- Protected test ----
app.MapGet("/api/me", (System.Security.Claims.ClaimsPrincipal user) =>
{
    return Results.Ok(new
    {
        email = user.FindFirst("email")?.Value
    });
}).RequireAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();
