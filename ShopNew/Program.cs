using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;
using ShopNew.Models;

namespace ShopNew
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // MongoDB configuration
            var mongoConn = builder.Configuration["Mongo:ConnectionString"] ?? builder.Configuration.GetConnectionString("Mongo");
            var mongoDbName = builder.Configuration["Mongo:DatabaseName"] ?? "ShopNewDb";
            var mongoClient = new MongoClient(mongoConn);
            builder.Services.AddSingleton<IMongoClient>(mongoClient);
            builder.Services.AddSingleton(sp => mongoClient.GetDatabase(mongoDbName));
            builder.Services.AddSingleton<IMongoCollection<Product>>(sp => sp.GetRequiredService<IMongoDatabase>().GetCollection<Product>("products"));

            var app = builder.Build();

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

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
