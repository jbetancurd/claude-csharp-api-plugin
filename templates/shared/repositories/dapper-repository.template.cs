// Template: Generic Dapper Repository with Specifications
// Copy and customize for your entity type

using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace YourApp.Infrastructure.Repositories
{
    /// <summary>
    /// Generic Dapper repository for lightweight, high-control data access.
    /// Use when: You need SQL control, performance optimization, or working with legacy DBs.
    /// </summary>
    public abstract class DapperRepository<T> where T : class
    {
        protected readonly IDbConnection _connection;
        protected abstract string TableName { get; }
        protected abstract string SelectAllQuery { get; }

        public DapperRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        /// <summary>
        /// Get entity by ID
        /// </summary>
        public virtual async Task<T> GetByIdAsync(int id)
        {
            const string query = "SELECT * FROM {0} WHERE Id = @Id";
            var sql = string.Format(query, TableName);
            return await _connection.QueryFirstOrDefaultAsync<T>(sql, new { Id = id });
        }

        /// <summary>
        /// Get all entities
        /// </summary>
        public virtual async Task<List<T>> GetAllAsync()
        {
            var entities = await _connection.QueryAsync<T>(SelectAllQuery);
            return entities.ToList();
        }

        /// <summary>
        /// Find entities by specification (custom query)
        /// </summary>
        public virtual async Task<List<T>> FindAsync(Specification<T> specification)
        {
            var sql = specification.BuildQuery(TableName);
            var parameters = specification.GetParameters();
            var entities = await _connection.QueryAsync<T>(sql, parameters);
            return entities.ToList();
        }

        /// <summary>
        /// Add new entity
        /// </summary>
        public virtual async Task<int> AddAsync(T entity)
        {
            var sql = GenerateInsertQuery(entity);
            var result = await _connection.ExecuteAsync(sql, entity);
            return result;
        }

        /// <summary>
        /// Add multiple entities in transaction
        /// </summary>
        public virtual async Task<int> AddRangeAsync(IEnumerable<T> entities)
        {
            using var transaction = _connection.BeginTransaction();
            try
            {
                int totalAdded = 0;
                foreach (var entity in entities)
                {
                    var sql = GenerateInsertQuery(entity);
                    totalAdded += await _connection.ExecuteAsync(sql, entity, transaction);
                }
                transaction.Commit();
                return totalAdded;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Update entity
        /// </summary>
        public virtual async Task<int> UpdateAsync(T entity)
        {
            var sql = GenerateUpdateQuery(entity);
            return await _connection.ExecuteAsync(sql, entity);
        }

        /// <summary>
        /// Delete entity
        /// </summary>
        public virtual async Task<int> DeleteAsync(int id)
        {
            var sql = $"DELETE FROM {TableName} WHERE Id = @Id";
            return await _connection.ExecuteAsync(sql, new { Id = id });
        }

        /// <summary>
        /// Count entities matching specification
        /// </summary>
        public virtual async Task<int> CountAsync(Specification<T> specification)
        {
            var sql = specification.BuildCountQuery(TableName);
            var parameters = specification.GetParameters();
            return await _connection.QueryFirstAsync<int>(sql, parameters);
        }

        /// <summary>
        /// Check if entity exists
        /// </summary>
        public virtual async Task<bool> ExistsAsync(int id)
        {
            var sql = $"SELECT 1 FROM {TableName} WHERE Id = @Id";
            var result = await _connection.QueryFirstOrDefaultAsync(sql, new { Id = id });
            return result != null;
        }

        protected virtual string GenerateInsertQuery(T entity)
        {
            // Override in derived class with specific columns
            throw new NotImplementedException("Override GenerateInsertQuery in derived class");
        }

        protected virtual string GenerateUpdateQuery(T entity)
        {
            // Override in derived class with specific columns
            throw new NotImplementedException("Override GenerateUpdateQuery in derived class");
        }
    }

    /// <summary>
    /// Example: Order Repository implementation
    /// </summary>
    public class OrderRepository : DapperRepository<Order>
    {
        protected override string TableName => "Orders";

        protected override string SelectAllQuery =>
            @"SELECT Id, OrderNumber, Status, CustomerId, Total, CreatedAt, UpdatedAt
              FROM Orders
              ORDER BY CreatedAt DESC";

        public OrderRepository(IDbConnection connection) : base(connection)
        {
        }

        protected override string GenerateInsertQuery(Order entity)
        {
            return @"INSERT INTO Orders (OrderNumber, Status, CustomerId, Total, CreatedAt)
                     VALUES (@OrderNumber, @Status, @CustomerId, @Total, @CreatedAt)";
        }

        protected override string GenerateUpdateQuery(Order entity)
        {
            return @"UPDATE Orders
                     SET Status = @Status, Total = @Total, UpdatedAt = @UpdatedAt
                     WHERE Id = @Id";
        }

        /// <summary>
        /// Find orders by customer with items
        /// </summary>
        public async Task<List<OrderWithItems>> GetOrdersByCustomerWithItemsAsync(int customerId)
        {
            const string query = @"
                SELECT o.*, oi.Id as ItemId, oi.ProductId, oi.Quantity
                FROM Orders o
                LEFT JOIN OrderItems oi ON o.Id = oi.OrderId
                WHERE o.CustomerId = @CustomerId
                ORDER BY o.CreatedAt DESC";

            using var reader = await _connection.QueryMultipleAsync(query, new { CustomerId = customerId });

            var ordersDict = new Dictionary<int, OrderWithItems>();
            var orders = reader.Read<OrderWithItems>();

            foreach (var order in orders)
            {
                if (!ordersDict.ContainsKey(order.Id))
                {
                    ordersDict[order.Id] = order;
                }
            }

            return ordersDict.Values.ToList();
        }

        /// <summary>
        /// Get pending orders (status-based search)
        /// </summary>
        public async Task<List<Order>> GetPendingOrdersAsync()
        {
            const string query = @"
                SELECT * FROM Orders
                WHERE Status = @Status
                ORDER BY CreatedAt ASC";

            var orders = await _connection.QueryAsync<Order>(query, new { Status = OrderStatus.Pending });
            return orders.ToList();
        }
    }

    /// <summary>
    /// Specification pattern for building dynamic queries
    /// </summary>
    public abstract class Specification<T> where T : class
    {
        protected List<(string Column, object Value)> _criteria = new();
        protected List<string> _orderBy = new();
        protected int? _take;
        protected int? _skip;

        public virtual string BuildQuery(string tableName)
        {
            var sql = $"SELECT * FROM {tableName}";

            if (_criteria.Any())
            {
                var where = string.Join(" AND ", _criteria.Select(c => $"{c.Column} = @{c.Column}"));
                sql += $" WHERE {where}";
            }

            if (_orderBy.Any())
            {
                sql += " ORDER BY " + string.Join(", ", _orderBy);
            }

            if (_skip.HasValue)
                sql += $" OFFSET {_skip.Value} ROWS";

            if (_take.HasValue)
                sql += $" FETCH NEXT {_take.Value} ROWS ONLY";

            return sql;
        }

        public virtual string BuildCountQuery(string tableName)
        {
            var sql = $"SELECT COUNT(*) FROM {tableName}";
            if (_criteria.Any())
            {
                var where = string.Join(" AND ", _criteria.Select(c => $"{c.Column} = @{c.Column}"));
                sql += $" WHERE {where}";
            }
            return sql;
        }

        public virtual DynamicParameters GetParameters()
        {
            var parameters = new DynamicParameters();
            foreach (var (column, value) in _criteria)
            {
                parameters.Add($"@{column}", value);
            }
            return parameters;
        }

        protected void AddCriteria(string column, object value)
        {
            _criteria.Add((column, value));
        }

        protected void AddOrderBy(string orderByExpression)
        {
            _orderBy.Add(orderByExpression);
        }

        protected void SetPaging(int skip, int take)
        {
            _skip = skip;
            _take = take;
        }
    }

    /// <summary>
    /// Example: Specification for active orders
    /// </summary>
    public class ActiveOrdersSpecification : Specification<Order>
    {
        public ActiveOrdersSpecification(int customerId, OrderStatus status = OrderStatus.Pending)
        {
            AddCriteria("CustomerId", customerId);
            AddCriteria("Status", status);
            AddOrderBy("CreatedAt DESC");
        }
    }
}
