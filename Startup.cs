using WalletNet.Data;
using WalletNet.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using WalletNet.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Configuration;

public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        // Add controllers
        services.AddControllers();

        // Learn more about configuring Swagger
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        // Add DbContext
        
        var connectionString = Configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Add Identity
        services.AddIdentity<User, IdentityRole>(options =>
        {
            options.Password.RequiredLength = 6;
            options.Password.RequireDigit = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // Add JWT Authentication
        var jwtSettings = Configuration.GetSection("JWT");
        var key = Encoding.ASCII.GetBytes(jwtSettings["Secret"]);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                ClockSkew = TimeSpan.Zero
            };
        })
        .AddGoogle(options =>
        {
            options.ClientId = Configuration["Authentication:Google:ClientId"];
            options.ClientSecret = Configuration["Authentication:Google:ClientSecret"];
        });

        // Register Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmailService, EmailService>();

        // CORS configuration
        services.AddCors(options =>
        {
            options.AddPolicy("AllowSpecificOrigin",
                policy => policy.WithOrigins("https://yourfrontend.com")
                                .AllowAnyHeader()
                                .AllowAnyMethod());
        });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();

            // Use CORS with any origin in development
            app.UseCors(policy => policy
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());
        }
        else
        {
            // Use CORS with specific origin in production
            app.UseCors("AllowSpecificOrigin");

            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseRouting();

        // Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });

        // Initialize database and create roles if needed
        using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
        {
            var services = serviceScope.ServiceProvider;
            try
            {
                var context = services.GetRequiredService<ApplicationDbContext>();
                var userManager = services.GetRequiredService<UserManager<User>>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

                // Ensure database is created and migrations are applied
                context.Database.Migrate();

                // Seed roles if needed
                if (!roleManager.RoleExistsAsync("Admin").Result)
                {
                    roleManager.CreateAsync(new IdentityRole("Admin")).Wait();
                }

                if (!roleManager.RoleExistsAsync("User").Result)
                {
                    roleManager.CreateAsync(new IdentityRole("User")).Wait();
                }

                // Seed admin user if needed
                var adminEmail = Configuration["AdminUser:Email"];
                if (adminEmail != null && !string.IsNullOrEmpty(adminEmail))
                {
                    var adminUser = userManager.FindByEmailAsync(adminEmail).Result;
                    if (adminUser == null)
                    {
                        adminUser = new User
                        {
                            UserName = adminEmail,
                            Email = adminEmail,
                            FirstName = "Admin",
                            LastName = "User",
                            IsEmailVerified = true
                        };

                        var adminPassword = Configuration["AdminUser:Password"];
                        if (!string.IsNullOrEmpty(adminPassword))
                        {
                            var result = userManager.CreateAsync(adminUser, adminPassword).Result;
                            if (result.Succeeded)
                            {
                                userManager.AddToRoleAsync(adminUser, "Admin").Wait();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Startup>>();
                logger.LogError(ex, "An error occurred while seeding the database.");
            }
        }
    }
}