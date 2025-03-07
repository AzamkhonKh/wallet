// using Microsoft.EntityFrameworkCore;
// using Scalar.AspNetCore;
// using WalletNet.Models;
// using Microsoft.AspNetCore.Authentication.JwtBearer;
// using Microsoft.IdentityModel.Tokens;
// using System.Text;
// using Microsoft.OpenApi.Models;
// using WalletNet.Services;
// using System.Runtime.InteropServices;

// var builder = WebApplication.CreateBuilder(args);

// // Add services to the container.
// // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

// builder.Services.AddScoped<TokenService>();
// builder.Services.AddControllers();

// // Load JWT settings from appsettings.json
// var jwtSettings = builder.Configuration.GetSection("JwtSettings");
// var secretKey = jwtSettings["SecretKey"] ?? "";
// var issuer = jwtSettings["Issuer"];
// var audience = jwtSettings["Audience"];
// var expirationMinutes = Convert.ToInt32(jwtSettings["ExpirationMinutes"]);

// // Get connection string from configurati   on
// var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
// builder.Services.AddDbContext<ApplicationDbContext>(options =>
//     options.UseNpgsql(connectionString));

// // builder.Services.AddIdentityApiEndpoints<User>();

// // Add authentication
// builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//     .AddJwtBearer(options =>
//     {
//         options.TokenValidationParameters = new TokenValidationParameters
//         {
//             ValidateIssuer = true,
//             ValidateAudience = true,
//             ValidateLifetime = true,
//             ValidateIssuerSigningKey = true,
//             ValidIssuer = issuer,
//             ValidAudience = audience,
//             IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
//         };
// #pragma warning restore CS8604 // Possible null reference argument.
//     });

// // Add authorization
// builder.Services.AddAuthorization();

// // Add Swagger
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen(options =>
// {
//     options.SwaggerDoc("v1", new OpenApiInfo
//     {
//         Title = "My API",
//         Version = "v1",
//         Description = "An ASP.NET Core API with Authentication"
//     });

//     // Add JWT Authentication to Swagger
//     options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
//     {
//         Description = "Enter 'Bearer {your JWT token}'",
//         Name = "Authorization",
//         In = ParameterLocation.Header,
//         Type = SecuritySchemeType.Http,
//         Scheme = "Bearer"
//     });

//     options.AddSecurityRequirement(new OpenApiSecurityRequirement
//     {
//         {
//             new OpenApiSecurityScheme
//             {
//                 Reference = new OpenApiReference
//                 {
//                     Type = ReferenceType.SecurityScheme,
//                     Id = "Bearer"
//                 }
//             },
//             new string[] {}
//         }
//     });
// });
// var app = builder.Build();

// // app.MapIdentityApi<User>();

// // Enable Swagger in development
// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI(options =>
//     {
//         options.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
//         options.RoutePrefix = ""; // Set Swagger UI at root URL
//     });
// }

// app.MapControllers();

// app.UseCors();
// app.UseAuthentication();
// app.UseAuthorization();

// app.Run();