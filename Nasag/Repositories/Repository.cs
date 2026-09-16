using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nasag.Data;

namespace Nasag.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    private readonly IDbContextFactory<NasaqDbContext> _factory;

    public Repository(IDbContextFactory<NasaqDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await ctx.Set<T>().AsNoTracking().ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<T>> WhereAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await ctx.Set<T>().AsNoTracking().Where(predicate).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<T?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await ctx.Set<T>().FindAsync(new object[] { id }, ct).AsTask().ConfigureAwait(false);
    }

    public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await ctx.Set<T>().AsNoTracking().FirstOrDefaultAsync(predicate, ct).ConfigureAwait(false);
    }

    public async Task<int> CountAsync(CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await ctx.Set<T>().CountAsync(ct).ConfigureAwait(false);
    }

    public async Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await ctx.Set<T>().CountAsync(predicate, ct).ConfigureAwait(false);
    }

    public async Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        ctx.Set<T>().Add(entity);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
        return entity;
    }

    public async Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        ctx.Set<T>().Update(entity);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(T entity, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        ctx.Set<T>().Remove(entity);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public IRepositoryScope<T> OpenScope()
    {
        var ctx = _factory.CreateDbContext();
        return new RepositoryScope<T>(ctx);
    }

    private sealed class RepositoryScope<TEntity> : IRepositoryScope<TEntity> where TEntity : class
    {
        private readonly NasaqDbContext _ctx;
        public RepositoryScope(NasaqDbContext ctx) { _ctx = ctx; }
        public IQueryable<TEntity> Query => _ctx.Set<TEntity>().AsNoTracking();
        public void Dispose() => _ctx.Dispose();
        public ValueTask DisposeAsync() => _ctx.DisposeAsync();
    }
}
