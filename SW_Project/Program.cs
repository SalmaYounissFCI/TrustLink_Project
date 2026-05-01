using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SW_Project.Data;
using SW_Project.Models;
using Rotativa.AspNetCore;

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

            builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
            builder.Services.AddTransient<IEmailSender, EmailSender>();
            // Identity with custom settings
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

            var app = builder.Build();
            RotativaConfiguration.Setup(app.Environment.WebRootPath, "Rotativa");

            // Apply migrations and seed data
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                dbContext.Database.Migrate();

                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                // == 1. إضافة التصنيفات إذا لم توجد ==
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
                    Console.WriteLine($"{categories.Count} categories added.");
                }

                // == 2. إنشاء مستخدم تجريبي (مالك) إذا لم يوجد ==
                var demoOwnerEmail = "owner@trustlink.com";
                var demoOwner = await userManager.FindByEmailAsync(demoOwnerEmail);
                if (demoOwner == null)
                {
                    demoOwner = new ApplicationUser
                    {
                        UserName = demoOwnerEmail,
                        Email = demoOwnerEmail,
                        Name = "Demo Owner",
                        Location = "Cairo",
                        CreatedAt = DateTime.Now,
                        IsActive = true,
                        Rating = 4.8m
                    };
                    var result = await userManager.CreateAsync(demoOwner, "Password123!");
                    if (result.Succeeded)
                        Console.WriteLine("Demo user created.");
                    else
                        Console.WriteLine("Failed to create demo user: " + string.Join(", ", result.Errors.Select(e => e.Description)));
                }

                // == 3. إضافة إعلانين تجريبيين إذا لم توجد إعلانات ==
                if (!dbContext.Listings.Any() && demoOwner != null)
                {
                    var electronicsCat = dbContext.Categories.FirstOrDefault(c => c.Name == "Electronics");
                    var cameraCat = dbContext.Categories.FirstOrDefault(c => c.Name == "Cameras & Photo");

                    var listings = new List<Listing>();

                    if (electronicsCat != null)
                    {
                        listings.Add(new Listing
                        {
                            Title = "Professional Camera",
                            Description = "Canon EOS 90D with lens kit. Perfect for photography enthusiasts.",
                            PricePerDay = 50,
                            Deposit = 200,
                            Location = "Cairo",
                            Status = "Available",
                            CategoryId = electronicsCat.Id,
                            OwnerId = demoOwner.Id,
                            CreatedAt = DateTime.Now
                        });
                    }

                    if (cameraCat != null)
                    {
                        listings.Add(new Listing
                        {
                            Title = "DSLR Camera Kit",
                            Description = "Nikon D3500, ideal for beginners. Includes bag and memory card.",
                            PricePerDay = 40,
                            Deposit = 150,
                            Location = "Alexandria",
                            Status = "Available",
                            CategoryId = cameraCat.Id,
                            OwnerId = demoOwner.Id,
                            CreatedAt = DateTime.Now
                        });
                    }

                    if (listings.Any())
                    {
                        dbContext.Listings.AddRange(listings);
                        await dbContext.SaveChangesAsync();
                        Console.WriteLine($"{listings.Count} demo listings added.");
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

            app.UseRouting();
            await app.RunAsync();
        }

    }

}