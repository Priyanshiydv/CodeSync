using System.Text;
using AuthService.Data;
using AuthService.Helpers;
using AuthService.Interfaces;
using AuthService.Repositories;
using AuthService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Database Connection
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(builder.Configuration
        .GetConnectionString("DefaultConnection")));

// Register Services
builder.Services.AddScoped<IAuthService, AuthServiceImpl>();
builder.Services.AddScoped<JwtHelper>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
// ADD — audit log service
builder.Services.AddScoped<AuditLogService>();
// JWT + Google + GitHub OAuth2 Authentication
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme =
        JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme =
        JwtBearerDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie("Cookies", options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
})
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
            Encoding.UTF8.GetBytes(jwtKey))
    };
})
// ADD — Google OAuth2
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["OAuth:Google:ClientId"]!;
    options.ClientSecret =
        builder.Configuration["OAuth:Google:ClientSecret"]!;
    // After Google login, redirect to our callback endpoint
    options.CallbackPath = "/signin-google";
})
// ADD — GitHub OAuth2
.AddGitHub(options =>
{
    options.ClientId = builder.Configuration["OAuth:GitHub:ClientId"]!;
    options.ClientSecret =
        builder.Configuration["OAuth:GitHub:ClientSecret"]!;
    options.CallbackPath = "/signin-github";
    // Request email scope from GitHub
    options.Scope.Add("user:email");
});

// Swagger with JWT
builder.Services.AddSwaggerGen(c =>
{
    c.OrderActionsBy(apiDesc =>
    {
        var path = apiDesc.RelativePath?.ToLower() ?? "";
        if (path.Contains("register")) return "1";
        if (path.Contains("login")) return "2";
        if (path.Contains("google")) return "3";
        if (path.Contains("github")) return "4";
        if (path.Contains("roles")) return "5";
        return "6";
    });

    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CodeSync Auth Service",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter: Bearer {your JWT token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// CORS for Angular — AllowCredentials required for OAuth2
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();