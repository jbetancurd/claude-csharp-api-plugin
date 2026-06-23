// Template: Redis Distributed Cache Implementation
// Use for: Multi-server deployments, microservices, high-traffic APIs

using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace YourApp.Infrastructure.Caching
{
    /// <summary>
    /// Distributed cache service using Redis
    /// Provides consistent interface for Redis cache
    /// Shared across multiple servers/services
    /// </summary>
    public interface IDistributedCacheService
    {
        Task<T> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        Task RemoveAsync(string key);
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
        Task SetManyAsync(Dictionary<string, object> items, TimeSpan? expiration = null);
        Task RemoveManyAsync(params string[] keys);
    }

    /// <summary>
    /// Redis Distributed Cache Implementation
    /// Data persisted in Redis server (survives app restart)
    /// Shared across all servers/instances
    /// Best for: Multi-server, microservices, high-traffic
    /// </summary>
    public class RedisCacheService : IDistributedCacheService
    {
        private readonly IDistributedCache _cache;
        private readonly JsonSerializerOptions _jsonOptions;

        public RedisCacheService(IDistributedCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _jsonOptions = new JsonSerializerOptions { PropertyNamesAreCamelCase = true };
        }

        /// <summary>
        /// Get value from Redis cache
        /// </summary>
        public async Task<T> GetAsync<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cache key required", nameof(key));

            var value = await _cache.GetStringAsync(key);

            if (value == null)
                return default;

            return JsonSerializer.Deserialize<T>(value, _jsonOptions);
        }

        /// <summary>
        /// Set value in Redis cache
        /// </summary>
        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cache key required", nameof(key));

            var json = JsonSerializer.Serialize(value, _jsonOptions);

            var options = new DistributedCacheEntryOptions();

            if (expiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiration;
            }
            else
            {
                // Default: 5 minutes if no expiration specified
                options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            }

            // SlidingExpiration: extends on access
            options.SlidingExpiration = TimeSpan.FromMinutes(1);

            await _cache.SetStringAsync(key, json, options);
        }

        /// <summary>
        /// Remove from Redis cache
        /// </summary>
        public async Task RemoveAsync(string key)
        {
            if (!string.IsNullOrEmpty(key))
                await _cache.RemoveAsync(key);
        }

        /// <summary>
        /// Get from cache, or set if missing
        /// Cache-aside pattern
        /// </summary>
        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            // Try to get from cache
            var cached = await GetAsync<T>(key);
            if (cached != null)
                return cached;

            // Not in cache, call factory to get value
            var value = await factory();

            // Store in cache
            if (value != null)
            {
                await SetAsync(key, value, expiration);
            }

            return value;
        }

        /// <summary>
        /// Set multiple values in cache (batch operation)
        /// </summary>
        public async Task SetManyAsync(Dictionary<string, object> items, TimeSpan? expiration = null)
        {
            if (items == null || items.Count == 0)
                return;

            var tasks = items.Select(kvp =>
                SetAsync(kvp.Key, kvp.Value, expiration)
            );

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Remove multiple keys from cache (batch operation)
        /// </summary>
        public async Task RemoveManyAsync(params string[] keys)
        {
            if (keys == null || keys.Length == 0)
                return;

            var tasks = keys.Select(RemoveAsync);
            await Task.WhenAll(tasks);
        }
    }

    // ============ EXAMPLE: TODO REDIS CACHE SERVICE ============

    public interface ITodoRedisCacheService
    {
        Task<List<Todo>> GetUserTodosAsync(int userId);
        Task<Todo> GetTodoAsync(int id);
        Task SetUserTodosAsync(int userId, List<Todo> todos);
        Task SetTodoAsync(Todo todo);
        Task InvalidateUserTodosAsync(int userId);
        Task InvalidateTodoAsync(int id);
    }

    public class TodoRedisCacheService : ITodoRedisCacheService
    {
        private readonly IDistributedCacheService _cache;
        private static readonly TimeSpan UserTodosTtl = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan TodoDetailTtl = TimeSpan.FromMinutes(10);

        public TodoRedisCacheService(IDistributedCacheService cache)
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

        public async Task InvalidateUserTodosAsync(int userId)
        {
            var key = $"todos:user:{userId}";
            await _cache.RemoveAsync(key);
        }

        public async Task InvalidateTodoAsync(int id)
        {
            var key = $"todos:detail:{id}";
            await _cache.RemoveAsync(key);
        }

        /// <summary>
        /// Invalidate all user-related caches when user is deleted
        /// </summary>
        public async Task InvalidateUserAllAsync(int userId)
        {
            await _cache.RemoveManyAsync(
                $"todos:user:{userId}",
                $"user:detail:{userId}",
                $"user:settings:{userId}"
            );
        }
    }

    // ============ REPOSITORY WITH REDIS CACHING ============

    public class RedisCachedTodoRepository : ITodoRepository
    {
        private readonly ITodoRepository _repository;
        private readonly ITodoRedisCacheService _cache;

        public RedisCachedTodoRepository(ITodoRepository repository, ITodoRedisCacheService cache)
        {
            _repository = repository;
            _cache = cache;
        }

        public async Task<Todo> GetByIdAsync(int id)
        {
            // Try Redis cache first
            var cached = await _cache.GetTodoAsync(id);
            if (cached != null)
                return cached;

            // Get from database
            var todo = await _repository.GetByIdAsync(id);
            if (todo != null)
            {
                // Cache in Redis for next time
                await _cache.SetTodoAsync(todo);
            }

            return todo;
        }

        public async Task<List<Todo>> GetByUserAsync(int userId)
        {
            // Try Redis cache first
            var cached = await _cache.GetUserTodosAsync(userId);
            if (cached != null)
                return cached;

            // Get from database
            var todos = await _repository.GetByUserAsync(userId);

            // Cache in Redis
            if (todos != null)
            {
                await _cache.SetUserTodosAsync(userId, todos);
            }

            return todos;
        }

        public async Task<int> AddAsync(Todo todo)
        {
            var id = await _repository.AddAsync(todo);

            // Invalidate user todos cache (stale now)
            await _cache.InvalidateUserTodosAsync(todo.UserId);

            return id;
        }

        public async Task UpdateAsync(Todo todo)
        {
            await _repository.UpdateAsync(todo);

            // Invalidate caches
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
    /// Register Redis distributed cache services
    /// Usage in Program.cs:
    /// builder.Services.AddRedisCache(builder.Configuration);
    /// </summary>
    public static class RedisCachingExtensions
    {
        public static IServiceCollection AddRedisCache(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var redisConnection = configuration.GetConnectionString("Redis")
                ?? throw new InvalidOperationException("Redis connection string not configured");

            // Add StackExchange.Redis distributed cache
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "TodoApp:";  // Key prefix
            });

            // Add cache service wrapper
            services.AddScoped<IDistributedCacheService, RedisCacheService>();

            // Add domain-specific caches
            services.AddScoped<ITodoRedisCacheService, TodoRedisCacheService>();

            // Wrap repository with caching
            services.AddScoped<ITodoRepository>(provider =>
            {
                var baseRepo = provider.GetRequiredService<TodoRepository>();
                var cache = provider.GetRequiredService<ITodoRedisCacheService>();
                return new RedisCachedTodoRepository(baseRepo, cache);
            });

            return services;
        }
    }

    // ============ APPSETTINGS CONFIGURATION ============

    /*
    {
      "ConnectionStrings": {
        "Redis": "localhost:6379"
      }
    }

    For production:
    {
      "ConnectionStrings": {
        "Redis": "redis-prod.example.com:6379,password=...,ssl=true"
      }
    }
    */

    // ============ HYBRID CACHING (IN-MEMORY + REDIS) ============

    /*
    For maximum performance with multiple servers:

    1. In-Memory Cache: Fast local access
    2. Redis Cache: Persistent, shared across servers

    Request
      ↓
    Check In-Memory Cache (fastest)
      ├─ Hit: Return immediately
      └─ Miss: Check Redis
          ├─ Hit: Load into memory, return
          └─ Miss: Load from DB, cache in both

    Implementation:
    - IMemoryCache for L1 (in-process)
    - IDistributedCache (Redis) for L2 (shared)
    - Implement cache-aside pattern
    - Invalidate both on data change
    */

    // ============ CLASSES FOR EXAMPLES ============

    public class Todo { public int Id { get; set; } public int UserId { get; set; } public string Title { get; set; } }
    public interface ITodoRepository { Task<Todo> GetByIdAsync(int id); Task<List<Todo>> GetByUserAsync(int userId); Task<int> AddAsync(Todo todo); Task UpdateAsync(Todo todo); Task DeleteAsync(int id); }
    public class TodoRepository : ITodoRepository { public Task<Todo> GetByIdAsync(int id) => throw new NotImplementedException(); public Task<List<Todo>> GetByUserAsync(int userId) => throw new NotImplementedException(); public Task<int> AddAsync(Todo todo) => throw new NotImplementedException(); public Task UpdateAsync(Todo todo) => throw new NotImplementedException(); public Task DeleteAsync(int id) => throw new NotImplementedException(); }
}
