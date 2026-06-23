// Template: CQRS Command/Query Pattern
// Copy and customize for your domain operations

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace YourApp.Application.CQRS
{
    // ============ COMMANDS (Write Operations) ============

    /// <summary>
    /// Base command interface
    /// </summary>
    public interface ICommand { }

    public interface ICommand<out TResponse> : ICommand { }

    /// <summary>
    /// Command Handler interface
    /// </summary>
    public interface ICommandHandler<in TCommand> where TCommand : ICommand
    {
        Task ExecuteAsync(TCommand command);
    }

    public interface ICommandHandler<in TCommand, TResponse> where TCommand : ICommand<TResponse>
    {
        Task<TResponse> ExecuteAsync(TCommand command);
    }

    // ============ QUERIES (Read Operations) ============

    /// <summary>
    /// Base query interface
    /// </summary>
    public interface IQuery<out TResponse> { }

    /// <summary>
    /// Query Handler interface
    /// </summary>
    public interface IQueryHandler<in TQuery, TResponse> where TQuery : IQuery<TResponse>
    {
        Task<TResponse> ExecuteAsync(TQuery query);
    }

    // ============ EXAMPLE: TODO CQRS ============

    // ------- COMMANDS -------

    /// <summary>
    /// Create a new todo
    /// </summary>
    public class CreateTodoCommand : ICommand<int>
    {
        public string Title { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// Complete a todo
    /// </summary>
    public class CompleteTodoCommand : ICommand
    {
        public int TodoId { get; set; }
    }

    /// <summary>
    /// Update todo
    /// </summary>
    public class UpdateTodoCommand : ICommand
    {
        public int TodoId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// Delete todo
    /// </summary>
    public class DeleteTodoCommand : ICommand
    {
        public int TodoId { get; set; }
    }

    // ------- QUERIES -------

    /// <summary>
    /// Get all todos for a user
    /// </summary>
    public class GetTodosQuery : IQuery<List<TodoItemReadModel>>
    {
        public int UserId { get; set; }
        public TodoStatus? FilterByStatus { get; set; }
    }

    /// <summary>
    /// Get single todo details
    /// </summary>
    public class GetTodoDetailQuery : IQuery<TodoDetailReadModel>
    {
        public int TodoId { get; set; }
    }

    /// <summary>
    /// Get todo statistics
    /// </summary>
    public class GetTodoStatsQuery : IQuery<TodoStatsReadModel>
    {
        public int UserId { get; set; }
    }

    // ------- READ MODELS (Denormalized for fast reads) -------

    /// <summary>
    /// Todo item for list view (optimized for reads)
    /// </summary>
    public class TodoItemReadModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public TodoStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    /// <summary>
    /// Todo detail view (more data)
    /// </summary>
    public class TodoDetailReadModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public TodoStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
    }

    /// <summary>
    /// Statistics view (aggregated data)
    /// </summary>
    public class TodoStatsReadModel
    {
        public int TotalTodos { get; set; }
        public int CompletedTodos { get; set; }
        public int PendingTodos { get; set; }
        public double CompletionPercentage { get; set; }
        public DateTime LastCompletedAt { get; set; }
    }

    public enum TodoStatus
    {
        Pending,
        Completed,
        Archived
    }

    // ============ COMMAND HANDLERS ============

    /// <summary>
    /// Handle CreateTodoCommand
    /// Contains business logic and persistence
    /// </summary>
    public class CreateTodoCommandHandler : ICommandHandler<CreateTodoCommand, int>
    {
        private readonly ITodoRepository _repository;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<CreateTodoCommandHandler> _logger;

        public CreateTodoCommandHandler(
            ITodoRepository repository,
            IEventPublisher eventPublisher,
            ILogger<CreateTodoCommandHandler> logger)
        {
            _repository = repository;
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        public async Task<int> ExecuteAsync(CreateTodoCommand command)
        {
            // Validate
            if (string.IsNullOrWhiteSpace(command.Title))
                throw new ArgumentException("Title required");

            // Create domain entity
            var todo = new Todo(command.Title, command.Description);

            // Save to write model
            var id = await _repository.AddAsync(todo);

            // Publish event for read model synchronization
            await _eventPublisher.PublishAsync(
                new TodoCreatedEvent { TodoId = id, Title = command.Title });

            _logger.LogInformation("Todo {TodoId} created", id);

            return id;
        }
    }

    public class CompleteTodoCommandHandler : ICommandHandler<CompleteTodoCommand>
    {
        private readonly ITodoRepository _repository;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<CompleteTodoCommandHandler> _logger;

        public CompleteTodoCommandHandler(
            ITodoRepository repository,
            IEventPublisher eventPublisher,
            ILogger<CompleteTodoCommandHandler> logger)
        {
            _repository = repository;
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        public async Task ExecuteAsync(CompleteTodoCommand command)
        {
            // Get todo
            var todo = await _repository.GetByIdAsync(command.TodoId)
                ?? throw new KeyNotFoundException($"Todo {command.TodoId} not found");

            // Apply business logic
            if (todo.IsCompleted)
                throw new InvalidOperationException("Todo already completed");

            todo.Complete();

            // Save
            await _repository.UpdateAsync(todo);

            // Publish event
            await _eventPublisher.PublishAsync(
                new TodoCompletedEvent { TodoId = command.TodoId });

            _logger.LogInformation("Todo {TodoId} completed", command.TodoId);
        }
    }

    // ============ QUERY HANDLERS ============

    /// <summary>
    /// Handle GetTodosQuery
    /// Only reads from denormalized read model
    /// No business logic, just data retrieval
    /// </summary>
    public class GetTodosQueryHandler : IQueryHandler<GetTodosQuery, List<TodoItemReadModel>>
    {
        private readonly ITodoReadModelRepository _readRepo;
        private readonly ILogger<GetTodosQueryHandler> _logger;

        public GetTodosQueryHandler(
            ITodoReadModelRepository readRepo,
            ILogger<GetTodosQueryHandler> logger)
        {
            _readRepo = readRepo;
            _logger = logger;
        }

        public async Task<List<TodoItemReadModel>> ExecuteAsync(GetTodosQuery query)
        {
            _logger.LogInformation("Fetching todos for user {UserId}", query.UserId);

            // Query optimized read model directly
            var todos = await _readRepo.GetTodosByUserAsync(
                query.UserId,
                query.FilterByStatus);

            return todos;
        }
    }

    public class GetTodoDetailQueryHandler : IQueryHandler<GetTodoDetailQuery, TodoDetailReadModel>
    {
        private readonly ITodoReadModelRepository _readRepo;

        public GetTodoDetailQueryHandler(ITodoReadModelRepository readRepo)
        {
            _readRepo = readRepo;
        }

        public async Task<TodoDetailReadModel> ExecuteAsync(GetTodoDetailQuery query)
        {
            var todo = await _readRepo.GetTodoDetailAsync(query.TodoId)
                ?? throw new KeyNotFoundException($"Todo {query.TodoId} not found");

            return todo;
        }
    }

    public class GetTodoStatsQueryHandler : IQueryHandler<GetTodoStatsQuery, TodoStatsReadModel>
    {
        private readonly ITodoReadModelRepository _readRepo;

        public GetTodoStatsQueryHandler(ITodoReadModelRepository readRepo)
        {
            _readRepo = readRepo;
        }

        public async Task<TodoStatsReadModel> ExecuteAsync(GetTodoStatsQuery query)
        {
            return await _readRepo.GetStatsAsync(query.UserId);
        }
    }

    // ============ DOMAIN EVENTS ============

    public interface IDomainEvent
    {
        DateTime OccurredAt { get; }
    }

    public class TodoCreatedEvent : IDomainEvent
    {
        public int TodoId { get; set; }
        public string Title { get; set; }
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }

    public class TodoCompletedEvent : IDomainEvent
    {
        public int TodoId { get; set; }
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }

    // ============ INTERFACES ============

    public interface ITodoRepository
    {
        Task<Todo> GetByIdAsync(int id);
        Task<int> AddAsync(Todo todo);
        Task UpdateAsync(Todo todo);
    }

    public interface ITodoReadModelRepository
    {
        Task<List<TodoItemReadModel>> GetTodosByUserAsync(int userId, TodoStatus? status = null);
        Task<TodoDetailReadModel> GetTodoDetailAsync(int todoId);
        Task<TodoStatsReadModel> GetStatsAsync(int userId);
    }

    public interface IEventPublisher
    {
        Task PublishAsync(IDomainEvent @event);
    }

    // ============ DOMAIN ENTITY ============

    public class Todo
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public TodoStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool IsCompleted => Status == TodoStatus.Completed;

        public Todo(string title, string description = "")
        {
            Title = title;
            Description = description;
            Status = TodoStatus.Pending;
            CreatedAt = DateTime.UtcNow;
        }

        public void Complete()
        {
            Status = TodoStatus.Completed;
            CompletedAt = DateTime.UtcNow;
        }
    }

    // ============ DISPATCHER PATTERN ============

    public interface ICommandDispatcher
    {
        Task ExecuteAsync<T>(T command) where T : ICommand;
        Task<TResponse> ExecuteAsync<T, TResponse>(T command) where T : ICommand<TResponse>;
    }

    public interface IQueryDispatcher
    {
        Task<TResponse> ExecuteAsync<T, TResponse>(T query) where T : IQuery<TResponse>;
    }
}
