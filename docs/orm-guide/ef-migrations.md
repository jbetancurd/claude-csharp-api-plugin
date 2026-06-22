# Entity Framework Core Migrations Guide

Migrations allow you to evolve your database schema over time while keeping your code changes in version control.

## Installation

```bash
# Install EF Core tools globally (do once)
dotnet tool install --global dotnet-ef

# Or install as project tool (recommended for teams)
dotnet add package Microsoft.EntityFrameworkCore.Tools
```

## Initial Setup

### 1. Create DbContext

Use the template at `templates/shared/repositories/ef-dbcontext.template.cs`

Key requirements:
```csharp
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Order> Orders { get; set; }
    public DbSet<Customer> Customers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

### 2. Create Design-Time Factory

Required for migrations to find your DbContext:

```csharp
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=MyApp;");
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
```

### 3. Add Connection String

In `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MyApp;Trusted_Connection=true;"
  }
}
```

### 4. Register DbContext

In `Program.cs`:

```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
```

## Creating Initial Migration

```bash
# Create migration with name
dotnet ef migrations add InitialCreate

# This creates:
# - Migrations/20240101120000_InitialCreate.cs
# - Migrations/ApplicationDbContextModelSnapshot.cs
```

## Applying Migrations

### Automatically at Startup (Development)

```csharp
// In Program.cs after building app
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();  // Applies pending migrations
}

app.Run();
```

### Manually

```bash
# Apply all pending migrations
dotnet ef database update

# Apply specific migration
dotnet ef database update 20240101120000_InitialCreate

# Rollback one migration
dotnet ef database update 20240101120001_PreviousMigration
```

## Common Migration Scenarios

### Scenario 1: Add New Entity

**1. Create domain entity:**
```csharp
public class Product : BaseEntity
{
    public string Name { get; set; }
    public decimal Price { get; set; }
}
```

**2. Add DbSet:**
```csharp
public DbSet<Product> Products { get; set; }
```

**3. Create entity configuration:**
```csharp
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(300);
        builder.Property(p => p.Price).HasPrecision(18, 2);
    }
}
```

**4. Generate migration:**
```bash
dotnet ef migrations add AddProductEntity
```

**5. Review generated migration** (in `Migrations/` folder)

**6. Apply migration:**
```bash
dotnet ef database update
```

### Scenario 2: Modify Existing Column

**1. Change entity property:**
```csharp
// Before
public string Email { get; set; }

// After
public string Email { get; set; }  // Made nullable, added index
public bool IsVerified { get; set; }
```

**2. Update configuration:**
```csharp
builder.Property(c => c.Email).HasMaxLength(255);
builder.HasIndex(c => c.Email).IsUnique();
```

**3. Generate migration:**
```bash
dotnet ef migrations add UpdateCustomerEmail
```

**4. Review and apply:**
```bash
dotnet ef database update
```

### Scenario 3: Add Foreign Key/Relationship

**1. Add navigation property:**
```csharp
public class Order : BaseEntity
{
    public int CustomerId { get; set; }
    public Customer Customer { get; set; }  // Navigation property
}
```

**2. Configure relationship:**
```csharp
builder.HasOne(o => o.Customer)
    .WithMany(c => c.Orders)
    .HasForeignKey(o => o.CustomerId)
    .OnDelete(DeleteBehavior.Cascade);
```

**3. Generate migration:**
```bash
dotnet ef migrations add AddCustomerToOrder
```

**4. Apply:**
```bash
dotnet ef database update
```

### Scenario 4: Add Column with Default Value

**1. Add property:**
```csharp
public bool IsActive { get; set; }  // Default: false
```

**2. Configure:**
```csharp
builder.Property(c => c.IsActive)
    .HasDefaultValue(true)
    .IsRequired();
```

**3. Generate migration:**
```bash
dotnet ef migrations add AddIsActiveToCustomer
```

The migration will set existing rows to the default value.

**4. Apply:**
```bash
dotnet ef database update
```

### Scenario 5: Add NOT NULL Column to Existing Table

**Problem**: Existing rows have no value for new column

**Solution 1: Make it nullable**
```csharp
public string Phone { get; set; }  // Nullable - safe
```

**Solution 2: Provide default**
```csharp
builder.Property(c => c.Phone)
    .HasMaxLength(20)
    .HasDefaultValue("");  // Default to empty string
```

**Solution 3: Manual migration with data backfill**

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // 1. Add nullable column
    migrationBuilder.AddColumn<string>(
        name: "Phone",
        table: "Customers",
        type: "nvarchar(20)",
        nullable: true);

    // 2. Set values for existing rows
    migrationBuilder.Sql(
        "UPDATE Customers SET Phone = '000-0000' WHERE Phone IS NULL");

    // 3. Make it NOT NULL
    migrationBuilder.AlterColumn<string>(
        name: "Phone",
        table: "Customers",
        type: "nvarchar(20)",
        nullable: false);
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropColumn(name: "Phone", table: "Customers");
}
```

## Migration Best Practices

✅ **Do:**
- Review migration SQL before applying
- Test migrations on copy of production database
- Keep migrations small and focused
- Name migrations descriptively: `AddProductEntity`, `UpdateCustomerEmail`
- Use `dotnet ef database update` to verify
- Commit migrations to version control

❌ **Don't:**
- Edit migration files after applying
- Skip migrations (always apply in order)
- Use raw SQL unless necessary
- Apply migrations during peak traffic
- Mix schema and data changes in one migration

## Debugging Migrations

### View Generated SQL

```bash
# See SQL that will be executed
dotnet ef migrations script
```

### List All Migrations

```bash
dotnet ef migrations list
```

### Remove Last Migration (if not applied)

```bash
# Remove from code, but database unchanged
dotnet ef migrations remove

# Then regenerate correct migration
dotnet ef migrations add CorrectName
```

### Reset Database to Clean State

```bash
# WARNING: Deletes all data!
dotnet ef database drop
dotnet ef database update  # Recreates from migrations
```

## Seeding Data

Add seed data in migration or in database initializer:

### In Migration

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.InsertData(
        table: "Customers",
        columns: new[] { "Name", "Email" },
        values: new object[] { "John Doe", "john@example.com" });
}
```

### In DbContext (Recommended)

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Customer>().HasData(
        new Customer { Id = 1, Name = "John Doe", Email = "john@example.com" });
}
```

### Using Initializer

```csharp
public static async Task SeedDataAsync(this ApplicationDbContext context)
{
    if (await context.Customers.AnyAsync())
        return;  // Already seeded

    // Add data here
    await context.Customers.AddAsync(new Customer { Name = "John" });
    await context.SaveChangesAsync();
}
```

## Production Deployment

### Before Deploying

1. **Test migrations locally:**
   ```bash
   dotnet ef database drop  # Clean local DB
   dotnet ef database update  # Test all migrations apply
   ```

2. **Review migration SQL:**
   ```bash
   dotnet ef migrations script
   ```

3. **Test on staging** with production-like data

4. **Create backup** of production database

### During Deployment

```csharp
// Option 1: Auto-migrate at startup (not recommended for large DBs)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}

// Option 2: Manual migration (safer)
// Run migration script manually before deploying app
dotnet ef database update --connection "Production connection string"
```

## Comparing Dapper vs EF Migrations

| Aspect | Dapper | EF Core |
|--------|--------|---------|
| **Migrations** | Manual SQL scripts | Automatic generation |
| **Version control** | SQL files | C# migration files |
| **Rollback** | Manual script | `dotnet ef database update` |
| **Data migration** | Manual SQL | C# or SQL in migration |
| **Complexity** | More control, more code | Less boilerplate |
| **Breaking changes** | Full control | EF warns about incompatibilities |

## Migration Naming Convention

```
Timestamp_DescriptiveAction.cs
├─ InitialCreate
├─ AddProductEntity
├─ UpdateCustomerEmail
├─ AddForeignKeyToOrder
├─ RemoveDeprecatedColumn
└─ SeedDefaultData
```

---

**Summary**: 
- EF migrations track schema changes in code
- Use `dotnet ef migrations add` to generate
- Use `dotnet ef database update` to apply
- Always review migration SQL
- Test before production deployment

