// Template: Entity Framework Core DbContext Configuration
// Copy and customize for your database context

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Reflection;

namespace YourApp.Infrastructure.Data
{
    /// <summary>
    /// DbContext - Entry point for EF Core
    ///
    /// Best Practices:
    /// - Configure all entities here
    /// - Use fluent API for mappings (not data annotations)
    /// - Keep configurations in separate files
    /// - Use shadow properties for metadata (CreatedAt, UpdatedAt)
    /// - Override SaveChangesAsync to set audit fields
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ============ DbSets (Entities) ============

        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Product> Products { get; set; }

        // ============ Configuration ============

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Apply all entity configurations from this assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            // Global conventions
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                // Soft delete - add IsDeleted property to all entities
                var isDeletedProperty = entityType.FindProperty("IsDeleted");
                if (isDeletedProperty != null && isDeletedProperty.ClrType == typeof(bool))
                {
                    // Add global query filter to exclude soft-deleted records
                    var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                    var propertyAccess = System.Linq.Expressions.Expression.Property(parameter, "IsDeleted");
                    var notDeletedExpression = System.Linq.Expressions.Expression.Equal(propertyAccess, System.Linq.Expressions.Expression.Constant(false));
                    var lambda = System.Linq.Expressions.Expression.Lambda(notDeletedExpression, parameter);
                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
                }
            }

            base.OnModelCreating(modelBuilder);
        }

        /// <summary>
        /// Override SaveChanges to set audit fields automatically
        /// </summary>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Set CreatedAt and UpdatedAt for audit trail
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is IAuditableEntity);

            foreach (var entry in entries)
            {
                var entity = (IAuditableEntity)entry.Entity;

                if (entry.State == EntityState.Added)
                {
                    entity.CreatedAt = DateTime.UtcNow;
                    entity.UpdatedAt = DateTime.UtcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entity.UpdatedAt = DateTime.UtcNow;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Override SaveChanges (non-async version)
        /// </summary>
        public override int SaveChanges()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is IAuditableEntity);

            foreach (var entry in entries)
            {
                var entity = (IAuditableEntity)entry.Entity;

                if (entry.State == EntityState.Added)
                {
                    entity.CreatedAt = DateTime.UtcNow;
                    entity.UpdatedAt = DateTime.UtcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entity.UpdatedAt = DateTime.UtcNow;
                }
            }

            return base.SaveChanges();
        }
    }

    // ============ Entity Configurations ============

    /// <summary>
    /// Order entity configuration
    /// Separate configuration from DbContext for clarity
    /// </summary>
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.HasKey(o => o.Id);

            builder.Property(o => o.OrderNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(o => o.Status)
                .HasConversion<int>()
                .HasDefaultValue(OrderStatus.Pending);

            builder.Property(o => o.Total)
                .HasPrecision(18, 2);

            builder.Property(o => o.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // Relationships
            builder.HasOne(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(o => o.Items)
                .WithOne(i => i.Order)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(o => o.CustomerId);
            builder.HasIndex(o => o.Status);
            builder.HasIndex(o => o.CreatedAt);

            // Table naming
            builder.ToTable("Orders");
        }
    }

    public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            builder.HasKey(i => i.Id);

            builder.Property(i => i.Quantity)
                .IsRequired();

            builder.Property(i => i.UnitPrice)
                .HasPrecision(18, 2);

            builder.HasIndex(i => i.OrderId);

            builder.ToTable("OrderItems");
        }
    }

    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(c => c.Email)
                .HasMaxLength(255);

            builder.HasIndex(c => c.Email);

            builder.HasMany(c => c.Orders)
                .WithOne(o => o.Customer);

            builder.ToTable("Customers");
        }
    }

    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(p => p.Price)
                .HasPrecision(18, 2);

            builder.HasIndex(p => p.Name);

            builder.ToTable("Products");
        }
    }

    // ============ Interfaces & Base Classes ============

    /// <summary>
    /// Audit interface - automatically tracks CreatedAt/UpdatedAt
    /// </summary>
    public interface IAuditableEntity
    {
        DateTime CreatedAt { get; set; }
        DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Base entity with common properties
    /// </summary>
    public abstract class BaseEntity : IAuditableEntity
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
    }

    // ============ Domain Classes ============

    public class Order : BaseEntity
    {
        public string OrderNumber { get; set; }
        public int CustomerId { get; set; }
        public decimal Total { get; set; }
        public OrderStatus Status { get; set; }

        // Navigation properties
        public Customer Customer { get; set; }
        public List<OrderItem> Items { get; set; } = new();

        // Domain behavior
        public void AddItem(OrderItem item)
        {
            Items.Add(item);
        }

        public void UpdateStatus(OrderStatus status)
        {
            Status = status;
        }
    }

    public class OrderItem : BaseEntity
    {
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        // Navigation
        public Order Order { get; set; }
    }

    public class Customer : BaseEntity
    {
        public string Name { get; set; }
        public string Email { get; set; }

        // Navigation
        public List<Order> Orders { get; set; } = new();
    }

    public class Product : BaseEntity
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Sku { get; set; }
    }

    public enum OrderStatus
    {
        Pending = 0,
        Approved = 1,
        Shipped = 2,
        Delivered = 3,
        Cancelled = 4
    }

    // ============ Factory Pattern for DbContext (for testing) ============

    /// <summary>
    /// Design-time factory for EF Core migrations
    /// Required for: dotnet ef migrations add InitialCreate
    /// </summary>
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            // Use SQL Server (or your target database)
            optionsBuilder.UseSqlServer(
                "Server=(localdb)\\mssqllocaldb;Database=YourAppDb;Trusted_Connection=true;");

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }

    // ============ Registration in DI Container ============

    /// <summary>
    /// Extension method for registering DbContext
    /// Add to Program.cs services:
    ///
    /// builder.Services.AddApplicationDatabase(builder.Configuration);
    /// </summary>
    public static class DatabaseServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationDatabase(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                var connectionString = configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException("Connection string not configured");

                options.UseSqlServer(
                    connectionString,
                    sqlServerOptions =>
                    {
                        sqlServerOptions.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelaySeconds: 5,
                            errorNumbersToAdd: null);
                    });

                // Enable query logging in development
                if (configuration.GetValue<bool>("LogQueries"))
                {
                    options.LogTo(Console.WriteLine);
                }
            });

            return services;
        }
    }

    // ============ Database Initialization (Seed Data) ============

    /// <summary>
    /// Initialize database with seed data
    /// Call from Program.cs after migration:
    ///
    /// using (var scope = app.Services.CreateScope())
    /// {
    ///     var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    ///     await db.SeedDataAsync();
    /// }
    /// </summary>
    public static class DatabaseInitializer
    {
        public static async Task SeedDataAsync(this ApplicationDbContext context)
        {
            if (await context.Customers.AnyAsync())
                return; // Database already seeded

            // Add seed data
            var customers = new List<Customer>
            {
                new Customer { Name = "John Doe", Email = "john@example.com" },
                new Customer { Name = "Jane Smith", Email = "jane@example.com" }
            };

            await context.Customers.AddRangeAsync(customers);

            var products = new List<Product>
            {
                new Product { Name = "Product A", Price = 29.99m, Sku = "PROD-001" },
                new Product { Name = "Product B", Price = 49.99m, Sku = "PROD-002" }
            };

            await context.Products.AddRangeAsync(products);

            await context.SaveChangesAsync();
        }
    }
}
