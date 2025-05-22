namespace WalletNet.Data;
using Microsoft.EntityFrameworkCore;
using WalletNet.Models;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // Define DbSets (tables) here
    public DbSet<User> Users { get; set; }
    public DbSet<Token> Tokens { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<BudgetMaster.Models.Space> Spaces { get; set; } // Added DbSet for Space


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // It's good practice to call base.OnModelCreating

        modelBuilder.Entity<User>().HasMany(u => u.RefreshTokens).WithOne(t => t.User).HasForeignKey(t => t.UserId);
        
        // Configure User-Space relationship
        modelBuilder.Entity<User>()
            .HasMany(u => u.Spaces)
            .WithOne(s => s.User)
            .HasForeignKey(s => s.UserId);

        // Configure Space-Transaction relationship
        // Assuming Transaction model will have SpaceId and Space navigation property
        modelBuilder.Entity<BudgetMaster.Models.Space>()
            .HasMany(s => s.Transactions)
            .WithOne(t => t.Space) // Assuming Transaction has a 'Space' navigation property
            .HasForeignKey(t => t.SpaceId); // Assuming Transaction has a 'SpaceId' foreign key

        // Configure User-Transaction relationship
        modelBuilder.Entity<User>()
            .HasMany(u => u.Transactions)
            .WithOne(t => t.User)
            .HasForeignKey(t => t.UserId);
    }
}
/*
internal sealed class BearerSecuritySchemeTransformer(Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (authenticationSchemes.Any(authScheme => authScheme.Name == "Bearer"))
        {
            var requirements = new Dictionary<string, OpenApiSecurityScheme>
            {
                ["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    In = ParameterLocation.Header,
                    BearerFormat = "Json Web Token"
                }
            };
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = requirements;

            foreach (var operation in document.Paths.Values.SelectMany(path => path.Operations))
            {
                operation.Value.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecurityScheme { Reference = new OpenApiReference { Id = "Bearer", Type = ReferenceType.SecurityScheme } }] = Array.Empty<string>()
                });
            }
        }
    }
}
*/