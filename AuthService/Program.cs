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

ConfigureServices(builder);
ConfigureAuthentication(builder);
ConfigureSwagger(builder);
ConfigureCors(builder);

var app = builder.Build();

ConfigurePipeline(app);
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    db.Database.Migrate();
}
await app.RunAsync();

// ------------------------------------------------------------
// Helper methods to reduce cognitive complexity
// ------------------------------------------------------------

void ConfigureServices(WebApplicationBuilder builder)
{
    // Database Connection
    builder.Services.AddDbContext<AuthDbContext>(options =>
        options.UseNpgsql(builder.Configuration
            .GetConnectionString("DefaultConnection")));

    // Register Services
    builder.Services.AddScoped<IAuthService, AuthServiceImpl>();
    builder.Services.AddScoped<JwtHelper>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<AuditLogService>();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
}

void ConfigureAuthentication(WebApplicationBuilder builder)
{
    var jwtKey = builder.Configuration["Jwt:Key"]!;

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
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
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["OAuth:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["OAuth:Google:ClientSecret"]!;
        options.CallbackPath = "/signin-google";
    })
    .AddGitHub(options =>
    {
        options.ClientId = builder.Configuration["OAuth:GitHub:ClientId"]!;
        options.ClientSecret = builder.Configuration["OAuth:GitHub:ClientSecret"]!;
        options.CallbackPath = "/signin-github";
        options.Scope.Add("user:email");
    });
}

void ConfigureSwagger(WebApplicationBuilder builder)
{
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
}

void ConfigureCors(WebApplicationBuilder builder)
{
    var frontendUrl = builder.Configuration["Frontend:BaseUrl"]!;
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAngular", policy =>
        {
            policy.WithOrigins(frontendUrl)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });
}

void ConfigurePipeline(WebApplication app)
{
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
}