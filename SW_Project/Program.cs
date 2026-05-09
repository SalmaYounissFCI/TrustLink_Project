using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SW_Project.Data;
using SW_Project.Models;
using SW_Project.Interfaces;
using SW_Project.Repositories;
using Rotativa.AspNetCore;
using Stripe; 
using SW_Project.Interfaces;  
using SW_Project.Services;     

namespace SW_Project
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Database Context
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // ✅ Register Repositories & UnitOfWork
            builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Email settings
            builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
            builder.Services.AddTransient<IEmailSender, EmailSender>();

            // Identity
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            // Cookie settings
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Home/AccessDenied";
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(7);
                options.SlidingExpiration = true;
            });

            builder.Services.AddControllersWithViews();

            builder.Services.AddScoped<IPaymentService, PaymentService>();


            var app = builder.Build();

            StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

            RotativaConfiguration.Setup(app.Environment.WebRootPath, "Rotativa");

           

            // Apply migrations and seed data
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await dbContext.Database.MigrateAsync();

                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                // Seed Categories
                if (!dbContext.Categories.Any())
                {
                    var categories = new List<Category>
                    {
                        new Category { Name = "Electronics", Icon = "bi-laptop" },
                        new Category { Name = "Furniture", Icon = "bi-house-door" },
                        new Category { Name = "Tools", Icon = "bi-wrench" },
                        new Category { Name = "Cameras & Photo", Icon = "bi-camera" },
                        new Category { Name = "Sports Equipment", Icon = "bi-bicycle" },
                        new Category { Name = "Books & Media", Icon = "bi-book" },
                        new Category { Name = "Party & Event Supplies", Icon = "bi-balloon" },
                        new Category { Name = "Vehicles", Icon = "bi-car-front" },
                        new Category { Name = "Clothing & Accessories", Icon = "bi-bag" },
                        new Category { Name = "Gardening", Icon = "bi-flower1" },
                        new Category { Name = "Musical Instruments", Icon = "bi-music-note-beamed" },
                        new Category { Name = "Handmade & Crafts", Icon = "bi-scissors" },
                        new Category { Name = "Office Equipment", Icon = "bi-printer" },
                        new Category { Name = "Pet Supplies", Icon = "bi-heart" },
                        new Category { Name = "Services", Icon = "bi-person-video" },
                        new Category { Name = "Outdoor & Camping", Icon = "bi-tree" },
                        new Category { Name = "Health & Beauty", Icon = "bi-heart-pulse" },
                        new Category { Name = "Baby & Kids", Icon = "bi-baby" },
                        new Category { Name = "Education & Tutoring", Icon = "bi-book-half" },
                        new Category { Name = "Photography Services", Icon = "bi-camera-reels" }
                    };
                    dbContext.Categories.AddRange(categories);
                    await dbContext.SaveChangesAsync();
                }

                // Create Admin role
                if (!await roleManager.RoleExistsAsync("Admin"))
                {
                    await roleManager.CreateAsync(new IdentityRole("Admin"));
                }

                // Create demo admin user
                var adminEmail = "owner@trustlink.com";
                var adminUser = await userManager.FindByEmailAsync(adminEmail);
                if (adminUser == null)
                {
                    adminUser = new ApplicationUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        Name = "System Administrator",
                        Location = "Head Office",
                        CreatedAt = DateTime.Now,
                        IsActive = true,
                        Rating = 5.0m
                    };
                    var result = await userManager.CreateAsync(adminUser, "Admin@123456");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, "Admin");
                    }
                }
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            await app.RunAsync();
        }
    }
}