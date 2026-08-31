using System.Text;
using EcomApi.Data;
using EcomApi.Mapping;
using EcomApi.Middleware;
using EcomApi.Repositories;
using EcomApi.Services;
using EcomApi.Services.NotificationChannels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Ecom API", Version = "v1", Description = "E-commerce REST API with Repository Pattern" });
});

builder.Services.AddDbContextPool<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")), poolSize: 128);

var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException("JWT_SECRET_KEY environment variable or Jwt:SecretKey configuration is required.");
var key = Encoding.UTF8.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

builder.Services.AddScoped<IPasswordHasher<EcomApi.Models.User>, PasswordHasher<EcomApi.Models.User>>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IWishlistRepository, WishlistRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IBannerRepository, BannerRepository>();
builder.Services.AddScoped<ICouponRepository, CouponRepository>();
builder.Services.AddScoped<IReturnRepository, ReturnRepository>();
builder.Services.AddScoped<IReturnPolicyRepository, ReturnPolicyRepository>();
builder.Services.AddScoped<ISupportRepository, SupportRepository>();
builder.Services.AddScoped<IBotService, BotService>();
builder.Services.AddScoped<IActivityRepository, ActivityRepository>();
builder.Services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<TwoFactorService>();
builder.Services.AddScoped<StripeGateway>();
builder.Services.AddScoped<PaymentGatewayFactory>();
builder.Services.AddScoped<PricingService>();
builder.Services.AddScoped<PaymentWebhookProcessor>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<AbandonedCartScanRunner>();

// Settings (admin-configurable, DB-backed with config fallback)
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ISettingRepository, SettingRepository>();
builder.Services.AddScoped<ISettingsProvider, SettingsProvider>();

// Email + multi-channel notifications (Strategy pattern)
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISmsProvider, NullSmsProvider>();
builder.Services.AddScoped<IWhatsAppProvider, NullWhatsAppProvider>();
builder.Services.AddScoped<INotificationChannel, EmailChannel>();
builder.Services.AddScoped<INotificationChannel, InAppChannel>();
builder.Services.AddScoped<INotificationChannel, SmsChannel>();
builder.Services.AddScoped<INotificationChannel, WhatsAppChannel>();

// Notification queue + background delivery (keeps HTTP requests off the email/SMS I/O path)
builder.Services.AddSingleton<INotificationQueue, NotificationQueue>();
builder.Services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
builder.Services.AddHostedService<NotificationBackgroundService>();
builder.Services.AddHostedService<AbandonedCartBackgroundService>();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? builder.Configuration["CorsOrigins"]?.Split(',', StringSplitOptions.RemoveEmptyEntries)
    ?? new[] { "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

MappingConfig.ConfigureMapping();

var app = builder.Build();

var runMigrations = builder.Configuration.GetValue<bool>("Migration:RunOnStartup");
var runSeeder = builder.Configuration.GetValue<bool>("Seed:Enabled");

if (runMigrations || runSeeder)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (runMigrations)
    {
        context.Database.Migrate();
    }

    if (runSeeder)
    {
        DbSeeder.Seed(context);
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ecom API v1"));
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RateLimitMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseCors("AllowAngular");
app.UseSession();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.UsePageViewMiddleware();

app.MapControllers();

app.Run();
