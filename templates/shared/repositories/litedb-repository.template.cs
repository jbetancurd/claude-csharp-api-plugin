// Template: LiteDB Repository Pattern
// Copy and customize for your entity type
// Use when: Embedded database, microservice, rapid prototyping

using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace YourApp.Infrastructure.Data
{
    /// <summary>
    /// Generic LiteDB repository
    ///
    /// Advantages:
    /// - No ORM setup (no DbContext, no migrations)
    /// - Single file database
    /// - Simple POCO entities
    /// - Built-in transactions
    /// - Perfect for microservices
    ///
    /// Best for: Microservices, desktop apps, rapid prototyping
    /// </summary>
    public abstract class LiteDbRepository<T> where T : class
    {
        protected readonly ILiteCollection<T> Collection;

        public LiteDbRepository(LiteDatabase db)
        {
            Collection = db.GetCollection<T>();
            OnCollectionCreated();
        }

        /// <summary>
        /// Override to create indexes and configure collection
        /// </summary>
        protected virtual void OnCollectionCreated()
        {
            // Create indexes for performance
            // Override in derived class:
            // Collection.EnsureIndex(x => x.Status);
        }

        /// <summary>
        /// Get entity by ID
        /// </summary>
        public virtual T GetById(int id)
        {
            return Collection.FindById(id);
        }

        /// <summary>
        /// Get all entities
        /// </summary>
        public virtual List<T> GetAll()
        {
            return Collection.FindAll().ToList();
        }

        /// <summary>
        /// Find entities matching predicate
        /// </summary>
        public virtual List<T> Find(Expression<Func<T, bool>> predicate)
        {
            return Collection.Find(predicate).ToList();
        }

        /// <summary>
        /// Find first matching entity
        /// </summary>
        public virtual T FirstOrDefault(Expression<Func<T, bool>> predicate)
        {
            return Collection.FindOne(predicate);
        }

        /// <summary>
        /// Insert new entity
        /// </summary>
        public virtual int Insert(T entity)
        {
            return Collection.Insert(entity);
        }

        /// <summary>
        /// Insert multiple entities
        /// </summary>
        public virtual BsonValue InsertBulk(IEnumerable<T> entities)
        {
            return Collection.InsertBulk(entities);
        }

        /// <summary>
        /// Update entity
        /// </summary>
        public virtual bool Update(T entity)
        {
            return Collection.Update(entity);
        }

        /// <summary>
        /// Delete entity by ID
        /// </summary>
        public virtual bool DeleteById(int id)
        {
            return Collection.Delete(id);
        }

        /// <summary>
        /// Delete entities matching predicate
        /// </summary>
        public virtual int Delete(Expression<Func<T, bool>> predicate)
        {
            return Collection.DeleteMany(predicate);
        }

        /// <summary>
        /// Count all entities
        /// </summary>
        public virtual int Count()
        {
            return Collection.Count();
        }

        /// <summary>
        /// Count entities matching predicate
        /// </summary>
        public virtual int Count(Expression<Func<T, bool>> predicate)
        {
            return Collection.Count(predicate);
        }

        /// <summary>
        /// Check if entity exists
        /// </summary>
        public virtual bool Exists(Expression<Func<T, bool>> predicate)
        {
            return Collection.Exists(predicate);
        }

        /// <summary>
        /// Get paginated results
        /// </summary>
        public virtual List<T> GetPaged(int page, int pageSize, Expression<Func<T, bool>> predicate = null)
        {
            var skip = (page - 1) * pageSize;
            var query = predicate == null
                ? Collection.FindAll()
                : Collection.Find(predicate);

            return query.Skip(skip).Limit(pageSize).ToList();
        }
    }

    // ============ EXAMPLE: TODO REPOSITORY ============

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
        Pending = 0,
        Completed = 1,
        Archived = 2
    }

    public interface ITodoRepository
    {
        Todo GetById(int id);
        List<Todo> GetAll();
        List<Todo> GetPending();
        List<Todo> GetCompleted();
        int Add(Todo todo);
        bool Update(Todo todo);
        bool Delete(int id);
        int GetPendingCount();
    }

    /// <summary>
    /// Todo Repository using LiteDB
    /// </summary>
    public class TodoRepository : LiteDbRepository<Todo>, ITodoRepository
    {
        public TodoRepository(LiteDatabase db) : base(db)
        {
        }

        /// <summary>
        /// Create indexes for performance
        /// </summary>
        protected override void OnCollectionCreated()
        {
            // Create indexes on frequently queried fields
            Collection.EnsureIndex(t => t.Status);
            Collection.EnsureIndex(t => t.CreatedAt);
            Collection.EnsureIndex(t => t.CompletedAt);
        }

        /// <summary>
        /// Get pending todos
        /// </summary>
        public List<Todo> GetPending()
        {
            return Find(t => t.Status == TodoStatus.Pending)
                .OrderBy(t => t.CreatedAt)
                .ToList();
        }

        /// <summary>
        /// Get completed todos
        /// </summary>
        public List<Todo> GetCompleted()
        {
            return Find(t => t.Status == TodoStatus.Completed)
                .OrderByDescending(t => t.CompletedAt)
                .ToList();
        }

        /// <summary>
        /// Get count of pending todos
        /// </summary>
        public int GetPendingCount()
        {
            return Count(t => t.Status == TodoStatus.Pending);
        }

        /// <summary>
        /// Add new todo
        /// </summary>
        public int Add(Todo todo)
        {
            todo.CreatedAt = DateTime.UtcNow;
            return Insert(todo);
        }

        /// <summary>
        /// Delete todo by ID
        /// </summary>
        public bool Delete(int id)
        {
            return DeleteById(id);
        }
    }

    // ============ SERVICE LAYER ============

    public interface ITodoService
    {
        TodoDto GetTodo(int id);
        List<TodoDto> GetAll();
        List<TodoDto> GetPending();
        TodoDto CreateTodo(CreateTodoDto dto);
        bool UpdateTodo(int id, UpdateTodoDto dto);
        bool CompleteTodo(int id);
        bool DeleteTodo(int id);
    }

    public class TodoService : ITodoService
    {
        private readonly ITodoRepository _repository;
        private readonly ILogger<TodoService> _logger;

        public TodoService(ITodoRepository repository, ILogger<TodoService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public TodoDto GetTodo(int id)
        {
            var todo = _repository.GetById(id)
                ?? throw new KeyNotFoundException($"Todo {id} not found");
            return MapToDto(todo);
        }

        public List<TodoDto> GetAll()
        {
            return _repository.GetAll().ConvertAll(MapToDto);
        }

        public List<TodoDto> GetPending()
        {
            return _repository.GetPending().ConvertAll(MapToDto);
        }

        public TodoDto CreateTodo(CreateTodoDto dto)
        {
            var todo = new Todo
            {
                Title = dto.Title,
                Description = dto.Description,
                Status = TodoStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            var id = _repository.Add(todo);
            todo.Id = id;

            _logger.LogInformation("Todo {TodoId} created", id);
            return MapToDto(todo);
        }

        public bool UpdateTodo(int id, UpdateTodoDto dto)
        {
            var todo = _repository.GetById(id)
                ?? throw new KeyNotFoundException($"Todo {id} not found");

            todo.Title = dto.Title ?? todo.Title;
            todo.Description = dto.Description ?? todo.Description;

            return _repository.Update(todo);
        }

        public bool CompleteTodo(int id)
        {
            var todo = _repository.GetById(id)
                ?? throw new KeyNotFoundException($"Todo {id} not found");

            if (todo.Status == TodoStatus.Completed)
                throw new InvalidOperationException("Todo already completed");

            todo.Status = TodoStatus.Completed;
            todo.CompletedAt = DateTime.UtcNow;

            return _repository.Update(todo);
        }

        public bool DeleteTodo(int id)
        {
            return _repository.Delete(id);
        }

        private TodoDto MapToDto(Todo todo)
        {
            return new TodoDto
            {
                Id = todo.Id,
                Title = todo.Title,
                Description = todo.Description,
                Status = todo.Status.ToString(),
                CreatedAt = todo.CreatedAt,
                CompletedAt = todo.CompletedAt
            };
        }
    }

    // ============ DTOs ============

    public class CreateTodoDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
    }

    public class UpdateTodoDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
    }

    public class TodoDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    // ============ DI REGISTRATION ============

    /// <summary>
    /// Extension method for registering LiteDB services
    /// Usage in Program.cs:
    /// builder.Services.AddLiteDbServices();
    /// </summary>
    public static class LiteDbServiceCollectionExtensions
    {
        public static IServiceCollection AddLiteDbServices(
            this IServiceCollection services,
            string connectionString = "app.db")
        {
            // Register LiteDatabase singleton
            services.AddSingleton(provider =>
            {
                var db = new LiteDatabase(connectionString);
                return db;
            });

            // Register repositories
            services.AddScoped<ITodoRepository, TodoRepository>();

            // Register services
            services.AddScoped<ITodoService, TodoService>();

            return services;
        }
    }
}
