// Template: In-Memory Cache Implementation
// Use for: Single-server deployments, fast local caching

using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YourApp.Infrastructure.Caching
{
    /// <summary>
    /// Cache service wrapper for IMemoryCache
    /// Provides consistent cache interface
    /// </summary>
    public interface ICacheService
    {
        T Get<T>(string key);
        Task<T> GetAsync<T>(string key);
        void Set<T>(string key, T value, TimeSpan? expiration = null);
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        void Remove(string key);
        Task RemoveAsync(string key);
        T GetOrSet<T>(string key, Func<T> factory, TimeSpan? expiration = null);
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
    }

    /// <summary>
    /// In-Memory Cache Implementation
    /// Data stored in process memory (lost on restart)
    /// Best for: Single-server, moderate data size
    /// </summary>
    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private static readonly object _lock = new();

        public MemoryCacheService(IMemoryCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        /// <summary>
        /// Get from cache synchronously
        /// </summary>
        public T Get<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cache key required", nameof(key));

            return _cache.TryGetValue(key, out T value) ? value : default;
        }

        /// <summary>
        /// Get from cache asynchronously
        /// </summary>
        public Task<T> GetAsync<T>(string key)
        {
            // IMemoryCache is synchronous, wrap in Task
            return Task.FromResult(Get<T>(key));
        }

        /// <summary>
        /// Set value in cache
        /// </summary>
        public void Set<T>(string key, T value, TimeSpan? expiration = null)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cache key required", nameof(key));

            var options = new MemoryCacheEntryOptions();

            if (expiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiration;
            }
            else
            {
                // Default: 5 minutes if no expiration specified
                options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            }

            // Set sliding expiration (extends on access)
            options.SlidingExpiration = TimeSpan.FromMinutes(1);

            _cache.Set(key, value, options);
        }

        /// <summary>
        /// Set value in cache asynchronously
        /// </summary>
        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            Set(key, value, expiration);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Remove from cache
        /// </summary>
        public void Remove(string key)
        {
            if (!string.IsNullOrEmpty(key))
                _cache.Remove(key);
        }

        /// <summary>
        /// Remove from cache asynchronously
        /// </summary>
        public Task RemoveAsync(string key)
        {
            Remove(key);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Get from cache, or set if missing (with lock for thread safety)
        /// </summary>
        public T GetOrSet<T>(string key, Func<T> factory, TimeSpan? expiration = null)
        {
            if (_cache.TryGetValue(key, out T value))
                return value;

            lock (_lock)
            {
                // Double-check locking pattern
                if (_cache.TryGetValue(key, out value))
                    return value;

                value = factory();
                Set(key, value, expiration);
                return value;
            }
        }

        /// <summary>
        /// Get from cache, or set if missing (async version)
        /// </summary>
        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            if (_cache.TryGetValue(key, out T value))
                return value;

            lock (_lock)
            {
                if (_cache.TryGetValue(key, out value))
                    return value;

                // Can't use async in lock, so run factory
                var task = factory();
                task.Wait();
                value = task.Result;
                Set(key, value, expiration);
                return value;
            }
        }
    }

    // ============ EXAMPLE: TODO CACHE SERVICE ============

    public interface ITodoCacheService
    {
        Task<List<Todo>> GetUserTodosAsync(int userId);
        Task<Todo> GetTodoAsync(int id);
        Task SetUserTodosAsync(int userId, List<Todo> todos);
        Task SetTodoAsync(Todo todo);
        Task InvalidateUserTodosAsync(int userId);
        Task InvalidateTodoAsync(int id);
    }

    public class TodoCacheService : ITodoCacheService
    {
        private readonly ICacheService _cache;
        private static readonly TimeSpan UserTodosTtl = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan TodoDetailTtl = TimeSpan.FromMinutes(10);

        public TodoCacheService(ICacheService cache)
        {
            _cache = cache;
        }

        public async Task<List<Todo>> GetUserTodosAsync(int userId)
        {
            var key = $"todos:user:{userId}";
            return await _cache.GetAsync<List<Todo>>(key);
        }

        public async Task<Todo> GetTodoAsync(int id)
        {
            var key = $"todos:detail:{id}";
            return await _cache.GetAsync<Todo>(key);
        }

        public async Task SetUserTodosAsync(int userId, List<Todo> todos)
        {
            var key = $"todos:user:{userId}";
            await _cache.SetAsync(key, todos, UserTodosTtl);
        }

        public async Task SetTodoAsync(Todo todo)
        {
            var key = $"todos:detail:{todo.Id}";
            await _cache.SetAsync(key, todo, TodoDetailTtl);
        }

        /// <summary>
        /// Invalidate cached todos for user
        /// </summary>
        public async Task InvalidateUserTodosAsync(int userId)
        {
            var key = $"todos:user:{userId}";
            await _cache.RemoveAsync(key);
        }

        /// <summary>
        /// Invalidate cached todo detail
        /// </summary>
        public async Task InvalidateTodoAsync(int id)
        {
            var key = $"todos:detail:{id}";
            await _cache.RemoveAsync(key);
        }
    }

    // ============ REPOSITORY WITH CACHING ============

    public class CachedTodoRepository : ITodoRepository
    {
        private readonly ITodoRepository _repository;
        private readonly ITodoCacheService _cache;

        public CachedTodoRepository(ITodoRepository repository, ITodoCacheService cache)
        {
            _repository = repository;
            _cache = cache;
        }

        public async Task<Todo> GetByIdAsync(int id)
        {
            // Try cache first
            var cached = await _cache.GetTodoAsync(id);
            if (cached != null)
                return cached;

            // Get from database
            var todo = await _repository.GetByIdAsync(id);
            if (todo != null)
            {
                // Cache for next time
                await _cache.SetTodoAsync(todo);
            }

            return todo;
        }

        public async Task<List<Todo>> GetByUserAsync(int userId)
        {
            // Try cache first
            var cached = await _cache.GetUserTodosAsync(userId);
            if (cached != null)
                return cached;

            // Get from database
            var todos = await _repository.GetByUserAsync(userId);

            // Cache for next time
            if (todos != null)
            {
                await _cache.SetUserTodosAsync(userId, todos);
            }

            return todos;
        }

        public async Task<int> AddAsync(Todo todo)
        {
            var id = await _repository.AddAsync(todo);

            // Invalidate user todos cache (it's now stale)
            await _cache.InvalidateUserTodosAsync(todo.UserId);

            return id;
        }

        public async Task UpdateAsync(Todo todo)
        {
            await _repository.UpdateAsync(todo);

            // Invalidate both caches (detail and user list)
            await _cache.InvalidateTodoAsync(todo.Id);
            await _cache.InvalidateUserTodosAsync(todo.UserId);
        }

        public async Task DeleteAsync(int id)
        {
            var todo = await _repository.GetByIdAsync(id);
            await _repository.DeleteAsync(id);

            // Invalidate caches
            await _cache.InvalidateTodoAsync(id);
            if (todo != null)
            {
                await _cache.InvalidateUserTodosAsync(todo.UserId);
            }
        }
    }

    // ============ DI SETUP ============

    /// <summary>
    /// Register memory cache services
    /// Usage in Program.cs:
    /// builder.Services.AddMemoryCaching();
    /// </summary>
    public static class MemoryCachingExtensions
    {
        public static IServiceCollection AddMemoryCaching(this IServiceCollection services)
        {
            // Add IMemoryCache
            services.AddMemoryCache();

            // Add cache service wrapper
            services.AddScoped<ICacheService, MemoryCacheService>();

            // Add domain-specific caches
            services.AddScoped<ITodoCacheService, TodoCacheService>();

            // Wrap repository with caching
            services.AddScoped<ITodoRepository>(provider =>
            {
                var baseRepo = provider.GetRequiredService<TodoRepository>();
                var cache = provider.GetRequiredService<ITodoCacheService>();
                return new CachedTodoRepository(baseRepo, cache);
            });

            return services;
        }
    }

    // ============ USAGE IN SERVICE ============

    /*
    public class TodoService
    {
        private readonly ITodoRepository _repository;
        private readonly ICacheService _cache;

        public TodoService(ITodoRepository repository, ICacheService cache)
        {
            _repository = repository;
            _cache = cache;
        }

        public async Task<List<Todo>> GetUserTodosAsync(int userId)
        {
            // Cache-aside pattern
            return await _cache.GetOrSetAsync(
                key: $"todos:user:{userId}",
                factory: () => _repository.GetByUserAsync(userId),
                expiration: TimeSpan.FromMinutes(5)
            );
        }
    }
    */

    // ============ CACHE INVALIDATION PATTERNS ============

    /*
    // Pattern 1: Time-based (TTL)
    - Automatic expiration after set time
    - Pro: Simple, automatic cleanup
    - Con: Stale data possible

    // Pattern 2: Event-based
    - Invalidate on data change
    - Pro: Always fresh data
    - Con: More complex

    // Pattern 3: Dependency-based
    - Cache groups (user todos depend on user)
    - Pro: Smart invalidation
    - Con: Complex tracking

    // Pattern 4: Lazy invalidation
    - Check timestamp in database
    - Pro: Balance freshness/performance
    - Con: Extra DB queries
    */

    // ============ CLASSES FOR EXAMPLES ============

    public class Todo { public int Id { get; set; } public int UserId { get; set; } public string Title { get; set; } }
    public interface ITodoRepository { Task<Todo> GetByIdAsync(int id); Task<List<Todo>> GetByUserAsync(int userId); Task<int> AddAsync(Todo todo); Task UpdateAsync(Todo todo); Task DeleteAsync(int id); }
    public class TodoRepository : ITodoRepository { public Task<Todo> GetByIdAsync(int id) => throw new NotImplementedException(); public Task<List<Todo>> GetByUserAsync(int userId) => throw new NotImplementedException(); public Task<int> AddAsync(Todo todo) => throw new NotImplementedException(); public Task UpdateAsync(Todo todo) => throw new NotImplementedException(); public Task DeleteAsync(int id) => throw new NotImplementedException(); }
}
