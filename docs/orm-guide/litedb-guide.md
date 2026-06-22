# LiteDB Guide: Embedded NoSQL Database

LiteDB is a lightweight, embedded NoSQL document database for .NET. Perfect for microservices, desktop apps, and rapid prototyping.

## Installation

```bash
dotnet add package LiteDB
```

## Core Concepts

### What is LiteDB?

- **Embedded**: Single-file database (no separate server)
- **Document-oriented**: Stores C# objects as JSON documents
- **NoSQL**: Schema-less, flexible data model
- **Lightweight**: Minimal dependencies, small footprint
- **ACID**: Transactions supported
- **Easy**: No ORM configuration, migrations, or DbContext

## Basic Setup

### 1. Create Database Instance

```csharp
// Open database (creates file if doesn't exist)
var db = new LiteDatabase(@"todo-app.db");

// Get collection
var todos = db.GetCollection<Todo>("todos");
```

### 2. Define Entities (No ORM Needed)

```csharp
// Simple POCO - no attributes required!
public class Todo
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public TodoStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public enum TodoStatus
{
    Pending,
    Completed,
    Archived
}
```

### 3. CRUD Operations

```csharp
// INSERT
var todo = new Todo 
{ 
    Title = "Learn LiteDB",
    Status = TodoStatus.Pending,
    CreatedAt = DateTime.UtcNow
};
var id = todos.Insert(todo);

// READ
var todo = todos.FindById(1);
var allTodos = todos.FindAll().ToList();
var pending = todos.Find(t => t.Status == TodoStatus.Pending).ToList();

// UPDATE
todo.Status = TodoStatus.Completed;
todo.CompletedAt = DateTime.UtcNow;
todos.Update(todo);

// DELETE
todos.Delete(1);
```

## Repository Pattern with LiteDB

### Simple Repository

```csharp
public interface ITodoRepository
{
    Todo GetById(int id);
    List<Todo> GetAll();
    List<Todo> FindPending();
    int Add(Todo todo);
    bool Update(Todo todo);
    bool Delete(int id);
}

public class TodoRepository : ITodoRepository
{
    private readonly ILiteCollection<Todo> _collection;

    public TodoRepository(LiteDatabase db)
    {
        _collection = db.GetCollection<Todo>("todos");
        
        // Create indexes for performance
        _collection.EnsureIndex(t => t.Status);
        _collection.EnsureIndex(t => t.CreatedAt);
    }

    public Todo GetById(int id)
    {
        return _collection.FindById(id);
    }

    public List<Todo> GetAll()
    {
        return _collection.FindAll().ToList();
    }

    public List<Todo> FindPending()
    {
        return _collection.Find(t => t.Status == TodoStatus.Pending).ToList();
    }

    public int Add(Todo todo)
    {
        return _collection.Insert(todo);
    }

    public bool Update(Todo todo)
    {
        return _collection.Update(todo);
    }

    public bool Delete(int id)
    {
        return _collection.Delete(id);
    }
}
```

## DI Setup

```csharp
// Program.cs
var builder = WebApplicationBuilder.CreateBuilder(args);

// Register LiteDatabase
builder.Services.AddSingleton<LiteDatabase>(provider =>
{
    var connectionString = "todo-app.db";
    return new LiteDatabase(connectionString);
});

// Register repository
builder.Services.AddScoped<ITodoRepository, TodoRepository>();

var app = builder.Build();
```

## Advanced Features

### Indexes

```csharp
// Create index for performance
_collection.EnsureIndex(t => t.Status);
_collection.EnsureIndex(t => t.CreatedAt);

// Composite index
_collection.EnsureIndex(t => new { t.Status, t.CreatedAt });
```

### Transactions

```csharp
using (var trans = db.BeginTrans())
{
    try
    {
        // Multiple operations
        todos.Insert(todo1);
        todos.Insert(todo2);
        
        trans.Commit();
    }
    catch
    {
        trans.Rollback();
        throw;
    }
}
```

### Aggregation/Queries

```csharp
// Complex queries
var completed = todos
    .Find(t => t.Status == TodoStatus.Completed)
    .OrderByDescending(t => t.CompletedAt)
    .Take(10)
    .ToList();

// Count
var pendingCount = todos.Count(t => t.Status == TodoStatus.Pending);

// Delete multiple
todos.DeleteMany(t => t.Status == TodoStatus.Archived);
```

### Backup & Restore

```csharp
// Backup
db.Rebuild();  // Optimize and compact

// Or copy the file
File.Copy("todo-app.db", "todo-app-backup.db");
```

## LiteDB vs SQL vs Dapper

| Feature | LiteDB | SQL + EF | SQL + Dapper |
|---------|--------|----------|--------------|
| **Setup** | ✅ Instant | ⭐⭐⭐ Medium | ⭐⭐⭐ Medium |
| **Entities** | ✅ Simple POCOs | ⭐⭐ Config needed | ✅ Simple |
| **Migrations** | ✅ None | ❌ Required | ✅ Manual SQL |
| **Relationships** | ⭐⭐ Nested objects | ✅ Full support | ⭐⭐ Manual joins |
| **Deployment** | ✅ File only | ❌ Separate DB | ❌ Separate DB |
| **Scalability** | ⭐⭐ Embedded | ✅✅✅ Enterprise | ✅✅✅ Enterprise |
| **Multi-user** | ⭐⭐ Limited | ✅✅✅ Full | ✅✅✅ Full |
| **Flexibility** | ✅✅✅ Schema-less | ⭐⭐ Typed | ✅✅✅ Flexible |
| **Backup** | ✅ Copy file | ⭐⭐ Complex | ⭐⭐ Complex |

## When to Use LiteDB

✅ **Perfect for:**
- Microservices with local data
- Desktop applications
- Rapid prototyping
- Small to medium datasets (millions of records)
- Embedded scenarios (no separate DB server)
- Developer tools and utilities
- Mobile apps (via Xamarin)
- IoT applications
- Data caching/synchronization

❌ **Avoid for:**
- Enterprise systems with many concurrent users
- Multi-tenant applications
- Complex relational data (many joins)
- Distributed systems (needs server DB)
- Very large datasets (billions of records)

## LiteDB vs SQL Decision

Choose **LiteDB** if:
- No separate database server available
- Single-threaded or low concurrency
- Document-oriented data fits your model
- Quick setup is priority
- File-based backup is acceptable

Choose **SQL + EF/Dapper** if:
- Complex entity relationships
- Multi-user concurrent access
- Enterprise-grade reliability needed
- Distributed systems
- Scalability to enterprise scale

## Example: Todo Microservice with LiteDB

```csharp
// Minimal, self-contained microservice
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplicationBuilder.CreateBuilder(args);

        // One line to setup database
        builder.Services.AddSingleton(new LiteDatabase("todos.db"));
        builder.Services.AddScoped<ITodoRepository, TodoRepository>();
        builder.Services.AddScoped<ITodoService, TodoService>();

        var app = builder.Build();

        // API endpoints
        app.MapGet("/api/todos", (ITodoService svc) => 
            Results.Ok(svc.GetAll()));
        
        app.MapPost("/api/todos", async (CreateTodoDto dto, ITodoService svc) =>
        {
            var todo = await svc.CreateAsync(dto);
            return Results.Created($"/api/todos/{todo.Id}", todo);
        });

        app.Run();
    }
}
```

## File Size Considerations

LiteDB database file grows with data:
- Empty: ~20 KB
- 1000 documents: ~500 KB - 1 MB
- 1M documents: ~500 MB - 2 GB (depending on document size)

File is single, portable, and easy to backup!

## Performance Tips

✅ **Do:**
- Create indexes on frequently queried fields
- Use transactions for multiple operations
- Close database connection when done
- Regular backups of .db file
- Use `Rebuild()` occasionally to optimize

❌ **Don't:**
- Use for multi-server distributed systems
- Store large binary files (images, videos)
- Rely on without backups
- Ignore index creation

---

**See Also**:
- [Dapper Guide](dapper-vs-ef-comparison.md)
- [EF Core Guide](ef-migrations.md)
- [Decision Tree](../decision-tree.md)
