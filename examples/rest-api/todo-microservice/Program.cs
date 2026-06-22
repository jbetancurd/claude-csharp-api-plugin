// Complete Todo Microservice Example - REST API
// Project Structure:
// - src/Domain/          (Business logic, entities)
// - src/Application/     (Services, DTOs)
// - src/Infrastructure/  (Repositories, DbContext)
// - src/API/             (Controllers, Middleware)

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ============ DEPENDENCY INJECTION SETUP ============

// Add services to the container
builder.Services
    // Database
    .AddDbContext<TodoDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection") ??
            throw new InvalidOperationException("Connection string not found")))

    // Application Services
    .AddScoped<ITodoApplicationService, TodoApplicationService>()

    // Infrastructure (Repositories)
    .AddScoped<ITodoRepository, TodoRepository>()
    .AddScoped<IDomainServiceTodo, DomainServiceTodo>()

    // Controllers
    .AddControllers()

    // Logging
    .Services.AddLogging(config =>
    {
        config.AddConsole();
    });

// Add Polly for resilience (if calling external APIs)
builder.Services.AddHttpClient<ExternalServiceClient>()
    .AddTransientHttpErrorPolicy(p =>
        p.WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(2)))
    .AddTransientHttpErrorPolicy(p =>
        p.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));

var app = builder.Build();

// ============ MIDDLEWARE PIPELINE ============

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseMiddleware<GlobalExceptionMiddleware>();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Database migration on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
    dbContext.Database.Migrate();
}

app.Run();

// ============ DOMAIN LAYER ============

namespace TodoApp.Domain.Entities
{
    /// <summary>
    /// Domain Entity: Todo
    /// Contains pure business logic, encapsulates invariants
    /// </summary>
    public class Todo : Entity
    {
        public string Title { get; private set; }
        public string Description { get; private set; }
        public TodoStatus Status { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? CompletedAt { get; private set; }

        // Required for EF
        protected Todo() { }

        public Todo(string title, string description = "")
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title is required");

            Title = title;
            Description = description ?? "";
            Status = TodoStatus.Pending;
            CreatedAt = DateTime.UtcNow;
        }

        public void Complete()
        {
            if (Status == TodoStatus.Completed)
                throw new InvalidOperationException("Todo already completed");

            Status = TodoStatus.Completed;
            CompletedAt = DateTime.UtcNow;
        }

        public void Reopen()
        {
            if (Status == TodoStatus.Pending)
                throw new InvalidOperationException("Todo is already pending");

            Status = TodoStatus.Pending;
            CompletedAt = null;
        }

        public void Update(string title, string description)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title is required");

            Title = title;
            Description = description ?? "";
        }
    }

    public abstract class Entity
    {
        public int Id { get; protected set; }
    }

    public enum TodoStatus
    {
        Pending = 0,
        Completed = 1,
        Archived = 2
    }
}

// ============ APPLICATION LAYER ============

namespace TodoApp.Application.Services
{
    public interface ITodoApplicationService
    {
        Task<TodoDto> CreateAsync(CreateTodoDto dto);
        Task<TodoDto> GetAsync(int id);
        Task<List<TodoDto>> ListAsync();
        Task<TodoDto> CompleteAsync(int id);
        Task<TodoDto> UpdateAsync(int id, UpdateTodoDto dto);
        Task DeleteAsync(int id);
    }

    public class TodoApplicationService : ITodoApplicationService
    {
        private readonly ITodoRepository _repository;
        private readonly IDomainServiceTodo _domainService;
        private readonly ILogger<TodoApplicationService> _logger;

        public TodoApplicationService(
            ITodoRepository repository,
            IDomainServiceTodo domainService,
            ILogger<TodoApplicationService> logger)
        {
            _repository = repository;
            _domainService = domainService;
            _logger = logger;
        }

        public async Task<TodoDto> CreateAsync(CreateTodoDto dto)
        {
            var todo = new Todo(dto.Title, dto.Description);
            await _repository.AddAsync(todo);

            _logger.LogInformation("Todo created: {TodoId}", todo.Id);
            return MapToDto(todo);
        }

        public async Task<TodoDto> GetAsync(int id)
        {
            var todo = await _repository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Todo {id} not found");

            return MapToDto(todo);
        }

        public async Task<List<TodoDto>> ListAsync()
        {
            var todos = await _repository.GetAllAsync();
            return todos.ConvertAll(MapToDto);
        }

        public async Task<TodoDto> CompleteAsync(int id)
        {
            var todo = await _repository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Todo {id} not found");

            todo.Complete();
            await _repository.UpdateAsync(todo);

            _logger.LogInformation("Todo completed: {TodoId}", id);
            return MapToDto(todo);
        }

        public async Task<TodoDto> UpdateAsync(int id, UpdateTodoDto dto)
        {
            var todo = await _repository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Todo {id} not found");

            todo.Update(dto.Title, dto.Description);
            await _repository.UpdateAsync(todo);

            return MapToDto(todo);
        }

        public async Task DeleteAsync(int id)
        {
            await _repository.DeleteAsync(id);
            _logger.LogInformation("Todo deleted: {TodoId}", id);
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

    // DTOs
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
}

// ============ INFRASTRUCTURE LAYER ============

namespace TodoApp.Infrastructure.Data
{
    public interface ITodoRepository
    {
        Task<Todo> GetByIdAsync(int id);
        Task<List<Todo>> GetAllAsync();
        Task AddAsync(Todo todo);
        Task UpdateAsync(Todo todo);
        Task DeleteAsync(int id);
    }

    public class TodoRepository : ITodoRepository
    {
        private readonly TodoDbContext _context;

        public TodoRepository(TodoDbContext context)
        {
            _context = context;
        }

        public async Task<Todo> GetByIdAsync(int id)
        {
            return await _context.Todos.FindAsync(id);
        }

        public async Task<List<Todo>> GetAllAsync()
        {
            return await _context.Todos.OrderByDescending(t => t.CreatedAt).ToListAsync();
        }

        public async Task AddAsync(Todo todo)
        {
            _context.Todos.Add(todo);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Todo todo)
        {
            _context.Todos.Update(todo);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var todo = await _context.Todos.FindAsync(id);
            if (todo != null)
            {
                _context.Todos.Remove(todo);
                await _context.SaveChangesAsync();
            }
        }
    }

    public class TodoDbContext : DbContext
    {
        public DbSet<Todo> Todos { get; set; }

        public TodoDbContext(DbContextOptions<TodoDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Todo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });
        }
    }
}

// ============ PRESENTATION LAYER ============

namespace TodoApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class TodosController : ControllerBase
    {
        private readonly ITodoApplicationService _service;

        public TodosController(ITodoApplicationService service)
        {
            _service = service;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<TodoDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<TodoDto>>> GetAll()
        {
            var todos = await _service.ListAsync();
            return Ok(todos);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(TodoDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TodoDto>> GetById(int id)
        {
            try
            {
                var todo = await _service.GetAsync(id);
                return Ok(todo);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(TodoDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TodoDto>> Create([FromBody] CreateTodoDto dto)
        {
            var todo = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = todo.Id }, todo);
        }

        [HttpPost("{id}/complete")]
        [ProducesResponseType(typeof(TodoDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TodoDto>> Complete(int id)
        {
            try
            {
                var todo = await _service.CompleteAsync(id);
                return Ok(todo);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(TodoDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TodoDto>> Update(int id, [FromBody] UpdateTodoDto dto)
        {
            try
            {
                var todo = await _service.UpdateAsync(id, dto);
                return Ok(todo);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
    }
}

// ============ MIDDLEWARE ============

namespace TodoApp.API.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new { error = "Internal server error" });
            }
        }
    }
}

// ============ DOMAIN SERVICE ============

namespace TodoApp.Infrastructure.Services
{
    public interface IDomainServiceTodo
    {
        bool CanComplete(Todo todo);
    }

    public class DomainServiceTodo : IDomainServiceTodo
    {
        public bool CanComplete(Todo todo)
        {
            return todo.Status != TodoApp.Domain.Entities.TodoStatus.Completed;
        }
    }
}

// ============ EXTERNAL SERVICE CLIENT (with Polly) ============

namespace TodoApp.Infrastructure.Clients
{
    public class ExternalServiceClient
    {
        private readonly HttpClient _httpClient;

        public ExternalServiceClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> CallExternalApiAsync(string endpoint)
        {
            // Polly retry and circuit breaker applied automatically via DI
            var response = await _httpClient.GetAsync(endpoint);
            return await response.Content.ReadAsStringAsync();
        }
    }
}

// Required namespaces
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApp.Domain.Entities;
using TodoApp.Application.Services;
using TodoApp.Infrastructure.Data;
using TodoApp.Infrastructure.Services;
