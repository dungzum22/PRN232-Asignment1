using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ShopNew.Models;
using ShopNew.Services;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ShopNew.Data;
using Microsoft.AspNetCore.DataProtection;

namespace ShopNew
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // PostgreSQL configuration
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? 
                                 builder.Configuration.GetConnectionString("Default") ??
                                 throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));

            builder.Services.AddScoped<AuthService>();
            builder.Services.AddScoped<CartService>();
            builder.Services.AddScoped<PaymentService>();
            builder.Services.AddScoped<ProductService>();
            builder.Services.AddScoped<OrderService>();
            builder.Services.AddScoped<UserService>();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSession();
            
            // Configure Data Protection to use database for key storage
            builder.Services.AddDataProtection()
                .PersistKeysToDbContext<ApplicationDbContext>()
                .SetApplicationName("ShopNew");

            // JWT Configuration with improved error handling
            var jwtKey = builder.Configuration["Jwt:Key"] ?? 
                        builder.Configuration["JWT_KEY"] ?? 
                        builder.Configuration["Jwt__Key"];
                        
            // Debug logging for JWT configuration (remove in production)
            Console.WriteLine($"JWT Key found: {!string.IsNullOrEmpty(jwtKey)}");
            Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
            
            if (string.IsNullOrWhiteSpace(jwtKey))
            {
                // Generate a temporary key for development if none is provided
                if (builder.Environment.IsDevelopment())
                {
                    jwtKey = "TempDevKey123456789012345678901234567890123456789012345678901234567890";
                    Console.WriteLine("WARNING: Using temporary JWT key for development!");
                }
                else
                {
                    throw new InvalidOperationException("JWT Key is not configured. Please set Jwt__Key environment variable.");
                }
            }
            
            var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? 
                           builder.Configuration["JWT_ISSUER"] ?? 
                           builder.Configuration["Jwt__Issuer"] ?? 
                           "ShopNew";
            var jwtAudience = builder.Configuration["Jwt:Audience"] ?? 
                             builder.Configuration["JWT_AUDIENCE"] ?? 
                             builder.Configuration["Jwt__Audience"] ?? 
                             "ShopNewUsers";
            
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtIssuer,
                        ValidAudience = jwtAudience,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtKey ?? throw new InvalidOperationException("JWT Key is null")))
                    };
                });

            // Authorization Policies
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireRole(UserRoles.Admin));
                options.AddPolicy("UserOrAdmin", policy => policy.RequireRole(UserRoles.User, UserRoles.Admin));
            });

            var app = builder.Build();

            // Apply database migrations automatically
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                context.Database.Migrate();
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseSession();
            app.UseMiddleware<Middleware.JwtCookieAuthenticationMiddleware>();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
