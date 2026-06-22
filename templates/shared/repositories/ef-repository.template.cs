// Template: Entity Framework Core Repository with Specifications
// Copy and customize for your entity type
// Use when: You want LINQ, change tracking, complex relationships, or rapid development

using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace YourApp.Infrastructure.Data.Repositories
{
    /// <summary>
    /// Generic EF Core repository for feature-rich data access.
    /// Use when: You want LINQ, change tracking, lazy loading, complex relationships.
    /// </summary>
    public abstract class EFRepository<T> where T : class
    {
        protected readonly DbContext _context;
        protected readonly DbSet<T> _dbSet;

        public EFRepository(DbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        /// <summary>
        /// Get entity by ID (with change tracking)
        /// </summary>
        public virtual async Task<T> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        /// <summary>
        /// Get entity by ID (no tracking - read-only)
        /// </summary>
        public virtual async Task<T> GetByIdAsNoTrackingAsync(int id)
        {
            return await _dbSet.AsNoTracking().FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
        }

        /// <summary>
        /// Get all entities
        /// </summary>
        public virtual async Task<List<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        /// <summary>
        /// Get all entities (no tracking)
        /// </summary>
        public virtual async Task<List<T>> GetAllAsNoTrackingAsync()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        /// <summary>
        /// Find entities using specification
        /// </summary>
        public virtual async Task<List<T>> FindAsync(Specification<T> spec)
        {
            var query = ApplySpecification(spec);
            return await query.ToListAsync();
        }

        /// <summary>
        /// Find entities with LINQ expression
        /// </summary>
        public virtual async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        /// <summary>
        /// Find first matching entity
        /// </summary>
        public virtual async Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate);
        }

        /// <summary>
        /// Add new entity
        /// </summary>
        public virtual async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Add multiple entities
        /// </summary>
        public virtual async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _dbSet.AddRangeAsync(entities);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Update entity (change tracking)
        /// </summary>
        public virtual async Task UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Delete entity
        /// </summary>
        public virtual async Task DeleteAsync(T entity)
        {
            _dbSet.Remove(entity);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Delete entity by ID
        /// </summary>
        public virtual async Task DeleteByIdAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                _dbSet.Remove(entity);
                await SaveChangesAsync();
            }
        }

        /// <summary>
        /// Count entities matching predicate
        /// </summary>
        public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate = null)
        {
            return predicate == null
                ? await _dbSet.CountAsync()
                : await _dbSet.CountAsync(predicate);
        }

        /// <summary>
        /// Check if entity exists
        /// </summary>
        public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        /// <summary>
        /// Paged query with skip/take
        /// </summary>
        public virtual async Task<PagedResult<T>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            Expression<Func<T, bool>> predicate = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null)
        {
            var query = _dbSet.AsQueryable();

            if (predicate != null)
                query = query.Where(predicate);

            var totalCount = await query.CountAsync();

            if (orderBy != null)
                query = orderBy(query);

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<T>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        /// <summary>
        /// Save changes to database
        /// </summary>
        public virtual async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Apply specification to query
        /// </summary>
        protected virtual IQueryable<T> ApplySpecification(Specification<T> spec)
        {
            var query = _dbSet.AsQueryable();

            if (spec.Criteria != null)
                query = query.Where(spec.Criteria);

            query = spec.Includes.Aggregate(query, (current, include) => current.Include(include));

            if (spec.OrderBy != null)
                query = query.OrderBy(spec.OrderBy);

            if (spec.OrderByDescending != null)
                query = query.OrderByDescending(spec.OrderByDescending);

            if (spec.IsPagingEnabled)
                query = query.Skip(spec.Skip).Take(spec.Take);

            return query;
        }

        /// <summary>
        /// Detach entity from change tracking
        /// </summary>
        public virtual void Detach(T entity)
        {
            _context.Entry(entity).State = EntityState.Detached;
        }
    }

    /// <summary>
    /// Example: Order Repository using EF Core
    /// </summary>
    public interface IOrderRepository
    {
        Task<Order> GetByIdAsync(int id);
        Task<List<Order>> FindAsync(Specification<Order> spec);
        Task<List<Order>> GetByCustomerAsync(int customerId);
        Task<List<Order>> GetPendingOrdersAsync();
        Task AddAsync(Order order);
        Task UpdateAsync(Order order);
        Task DeleteAsync(int id);
        Task SaveChangesAsync();
    }

    public class OrderRepository : EFRepository<Order>, IOrderRepository
    {
        public OrderRepository(OrderDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Get orders by customer with items loaded
        /// </summary>
        public async Task<List<Order>> GetByCustomerAsync(int customerId)
        {
            var spec = new OrdersByCustomerSpecification(customerId);
            return await FindAsync(spec);
        }

        /// <summary>
        /// Get pending orders (status-based search)
        /// </summary>
        public async Task<List<Order>> GetPendingOrdersAsync()
        {
            return await _dbSet
                .Where(o => o.Status == OrderStatus.Pending)
                .Include(o => o.Items)
                .OrderBy(o => o.CreatedAt)
                .ToListAsync();
        }
    }

    /// <summary>
    /// EF Specification pattern for building dynamic queries
    /// </summary>
    public abstract class Specification<T> where T : class
    {
        public Expression<Func<T, bool>> Criteria { get; protected set; }
        public List<Expression<Func<T, object>>> Includes { get; } = new();
        public Expression<Func<T, object>> OrderBy { get; protected set; }
        public Expression<Func<T, object>> OrderByDescending { get; protected set; }

        public int Take { get; protected set; }
        public int Skip { get; protected set; }
        public bool IsPagingEnabled { get; protected set; }

        protected virtual void AddInclude(Expression<Func<T, object>> includeExpression)
        {
            Includes.Add(includeExpression);
        }

        protected virtual void AddOrderBy(Expression<Func<T, object>> orderByExpression)
        {
            OrderBy = orderByExpression;
        }

        protected virtual void AddOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression)
        {
            OrderByDescending = orderByDescendingExpression;
        }

        protected virtual void ApplyPaging(int skip, int take)
        {
            Skip = skip;
            Take = take;
            IsPagingEnabled = true;
        }
    }

    /// <summary>
    /// Example: Specification for orders by customer
    /// </summary>
    public class OrdersByCustomerSpecification : Specification<Order>
    {
        public OrdersByCustomerSpecification(int customerId)
        {
            Criteria = o => o.CustomerId == customerId;
            AddInclude(o => o.Items);
            AddInclude(o => o.Customer);
            AddOrderByDescending(o => o.CreatedAt);
        }
    }

    /// <summary>
    /// Paginated result
    /// </summary>
    public class PagedResult<T> where T : class
    {
        public List<T> Items { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }

    // ============ Domain Classes (for reference) ============

    public class Order
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public int CustomerId { get; set; }
        public decimal Total { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public Customer Customer { get; set; }
        public List<OrderItem> Items { get; set; } = new();
    }

    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<Order> Orders { get; set; } = new();
    }

    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        // Navigation
        public Order Order { get; set; }
    }

    public enum OrderStatus
    {
        Pending = 0,
        Approved = 1,
        Shipped = 2,
        Delivered = 3,
        Cancelled = 4
    }
}
