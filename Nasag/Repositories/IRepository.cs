using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Nasag.Repositories;

/// <summary>
/// Generic async repository over <typeparamref name="T"/>.
/// Each call opens a short-lived DbContext via <see cref="Microsoft.EntityFrameworkCore.IDbContextFactory{TContext}"/>.
/// </summary>
public interface IRepository<T> where T : class
{
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<T>> WhereAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    Task<T> AddAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task DeleteAsync(T entity, CancellationToken ct = default);

    /// <summary>For advanced read scenarios — returns an IQueryable bound to a fresh context.
    /// Caller MUST dispose the returned scope.</summary>
    IRepositoryScope<T> OpenScope();
}

public interface IRepositoryScope<T> : IDisposable, IAsyncDisposable where T : class
{
    IQueryable<T> Query { get; }
}
