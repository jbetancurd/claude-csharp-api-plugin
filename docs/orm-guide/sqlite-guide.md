# SQLite Guide: Embedded SQL Database

SQLite is a lightweight, embedded SQL database perfect for desktop apps, mobile, and small services. It combines SQL power with embedded simplicity.

## Installation

```bash
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
```

## Core Concepts

### What is SQLite?

- **Embedded**: Single-file database (no separate server)
- **SQL**: Full SQL support with ACID transactions
- **Relational**: Proper database schema and relationships
- **Zero Configuration**: Works out of the box
- **Portable**: Database is just a file (.db)
- **Lightweight**: Minimal dependencies

## SQLite vs Alternatives

| Feature | SQLite | LiteDB | SQL Server | PostgreSQL |
|---------|--------|--------|------------|------------|
| **Type** | SQL | NoSQL | SQL | SQL |
| **Server** | Embedded | Embedded | Server | Server |
| **Schema** | Relational | Schema-less | Relational | Relational |
| **Transactions** | ✅ ACID | ✅ ACID | ✅✅✅ | ✅✅ |
| **Concurrent Users** | ⭐ 1-5 | ⭐ 1-3 | ✅✅✅ Multi | ✅✅✅ Multi |
| **File Size** | ~500KB | ~20KB | Server-based | Server-based |
| **Relationships** | ✅ Full FK | ⭐ Nested | ✅ Full | ✅ Full |
| **Migrations** | ✅ EF Core | ❌ None | ✅ EF Core | ✅ EF Core |
| **Scaling** | ⭐ Limited | ⭐ Limited | ✅✅✅ | ✅✅✅ |
| **Development** | ⭐⭐⭐ Fast | ⭐⭐⭐ Fast | ⭐⭐ Setup | ⭐⭐ Setup |

## Basic Setup with EF Core

### 1. Create DbContext

```csharp
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public DbSet<Todo> Todos { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        // Path to SQLite database file
        options.UseSqlite("Data Source=app.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure entities
        modelBuilder.Entity<Todo>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Title).IsRequired().HasMaxLength(200);
            entity.HasOne(t => t.User)
                .WithMany(u => u.Todos)
                .HasForeignKey(t => t.UserId);
        });

        base.OnModelCreating(modelBuilder);
    }
}
```

### 2. Define Entities

```csharp
public class Todo
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public TodoStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Foreign key
    public int UserId { get; set; }
    public User User { get; set; }
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }

    // Navigation
    public List<Todo> Todos { get; set; } = new();
}

public enum TodoStatus
{
    Pending,
    Completed,
    Archived
}
```

### 3. Create Initial Migration

```bash
# Create migration
dotnet ef migrations add InitialCreate

# Apply migration (creates database)
dotnet ef database update
```

### 4. CRUD Operations

```csharp
using var context = new AppDbContext();

// CREATE
var todo = new Todo { Title = "Learn SQLite", UserId = 1 };
context.Todos.Add(todo);
context.SaveChanges();

// READ
var todo = context.Todos.Find(1);
var allTodos = context.Todos.ToList();
var pending = context.Todos.Where(t => t.Status == TodoStatus.Pending).ToList();

// UPDATE
todo.Status = TodoStatus.Completed;
context.SaveChanges();

// DELETE
context.Todos.Remove(todo);
context.SaveChanges();
```

## DI Setup

```csharp
// Program.cs
var builder = WebApplicationBuilder.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=app.db")
);

builder.Services.AddScoped<ITodoRepository, TodoRepository>();
builder.Services.AddScoped<ITodoService, TodoService>();

var app = builder.Build();
```

## SQLite Best Practices

### ✅ Do:
- **Use EF Core** - Best experience with SQLite
- **Create indexes** on frequently queried columns
- **Use transactions** for multiple operations
- **Enable foreign keys** - `PRAGMA foreign_keys = ON`
- **Regular backups** of .db file
- **Connection pooling** in production

### ❌ Don't:
- **High concurrency** - SQLite locks entire DB on writes
- **Multi-server deployment** - Not designed for shared access
- **Large datasets** - Scalability limited to ~500MB-1GB
- **Ignore migrations** - Use EF migrations like any SQL DB
- **Leave database file writable** by untrusted code

## Connection Strings

### Local Development
```csharp
"Data Source=app.db"
```

### With Password
```csharp
"Data Source=app.db;Password=mypassword"
```

### In-Memory (Testing)
```csharp
"Data Source=:memory:"
```

### Relative Path
```csharp
"Data Source=./data/app.db"
```

### Absolute Path
```csharp
"Data Source=/var/lib/app/app.db"
```

## Advanced Features

### Foreign Keys

```csharp
// Enable foreign key constraints (do this in DbContext)
protected override void OnConfiguring(DbContextOptionsBuilder options)
{
    options.UseSqlite("Data Source=app.db",
        sqliteOptions => sqliteOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));

    // Enable foreign keys
    var connection = (Microsoft.Data.Sqlite.SqliteConnection)Database.GetDbConnection();
    connection.Open();
    using var command = connection.CreateCommand();
    command.CommandText = "PRAGMA foreign_keys = ON";
    command.ExecuteNonQuery();
}
```

### Transactions

```csharp
using (var transaction = context.Database.BeginTransaction())
{
    try
    {
        context.Todos.Add(todo1);
        context.Todos.Add(todo2);
        context.SaveChanges();

        transaction.Commit();
    }
    catch
    {
        transaction.Rollback();
        throw;
    }
}
```

### Raw SQL

```csharp
// Execute raw SQL
var todos = context.Todos
    .FromSqlInterpolated($"SELECT * FROM Todos WHERE UserId = {userId}")
    .ToList();

// Execute command
context.Database.ExecuteSqlInterpolated(
    $"UPDATE Todos SET Status = 'Completed' WHERE UserId = {userId}"
);
```

## SQLite Migrations

### Create Migration
```bash
dotnet ef migrations add AddDescriptionColumn
```

### Apply Migrations
```bash
# Apply all pending
dotnet ef database update

# Apply to specific migration
dotnet ef database update AddDescriptionColumn
```

### Remove Migration
```bash
dotnet ef migrations remove
```

## Database File Management

### Backup
```csharp
// Copy the database file
File.Copy("app.db", "app-backup.db", overwrite: true);
```

### Verify
```bash
sqlite3 app.db ".tables"
sqlite3 app.db "SELECT count(*) FROM Todos;"
```

### Optimize
```csharp
// Vacuum and optimize
context.Database.ExecuteSqlRaw("VACUUM");
```

## When to Use SQLite

### ✅ Perfect for:
- Desktop applications (WPF, WinForms)
- Mobile apps (Xamarin, MAUI)
- Electron-based apps
- Small microservices (1-5 concurrent users)
- Development and testing
- Offline-first applications
- Embedded systems
- Single-user applications

### ❌ Avoid for:
- High concurrency (>5 concurrent users)
- Cloud applications requiring scalability
- Multi-server deployment
- Real-time analytics with heavy reads
- High-traffic APIs

## SQLite vs Server SQL Decision

Choose **SQLite** if:
- Embedded/single-file database is acceptable
- Limited concurrent users (1-5)
- No separate database server available
- Want SQL features with no setup
- Portability important (file-based)

Choose **Server SQL** (PostgreSQL, SQL Server) if:
- Multi-user concurrent access needed (10+)
- Enterprise scalability required
- Complex relationships and reporting
- High availability needed
- Multiple servers accessing same DB

## Common Issues & Solutions

### "database is locked"
```csharp
// Increase timeout
"Data Source=app.db;Timeout=25"

// Use WAL mode for better concurrency
"Data Source=app.db;Cache=Shared"
```

### Slow performance
```csharp
// Create indexes
modelBuilder.Entity<Todo>()
    .HasIndex(t => t.UserId);

modelBuilder.Entity<Todo>()
    .HasIndex(t => new { t.UserId, t.Status });
```

### Migration issues
```bash
# Reset (warning: deletes data!)
dotnet ef database drop
dotnet ef database update
```

## Example: Todo App with SQLite

```csharp
// Program.cs
var builder = WebApplicationBuilder.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=todos.db")
);

builder.Services.AddScoped<ITodoRepository, TodoRepository>();
builder.Services.AddScoped<ITodoService, TodoService>();
builder.Services.AddControllers();

var app = builder.Build();

// Auto-migrate on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();  // Create/update schema
}

app.MapControllers();
app.Run();
```

---

**See Also**:
- [LiteDB Guide](litedb-guide.md) - NoSQL alternative
- [Dapper vs EF Comparison](dapper-vs-ef-comparison.md)
- [Decision Tree](../decision-tree.md)
