using System.Linq.Expressions;
using BookExchange.Web.Data;
using BookExchange.Web.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookExchange.Web.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    private readonly BookExchangeDbContext _ctx;
    private readonly DbSet<T> _set;

    public Repository(BookExchangeDbContext ctx)
    {
        _ctx = ctx;
        _set = ctx.Set<T>();
    }

    public Task<T?> GetByIdAsync(object id) => _set.FindAsync(id).AsTask();

    public async Task<IReadOnlyList<T>> GetAllAsync() => await _set.AsNoTracking().ToListAsync();

    public IQueryable<T> Query() => _set.AsQueryable();

    public async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate)
        => await _set.AsNoTracking().Where(predicate).ToListAsync();

    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate) => _set.AnyAsync(predicate);

    public Task AddAsync(T entity) => _set.AddAsync(entity).AsTask();

    public void Update(T entity) => _set.Update(entity);

    public void Remove(T entity) => _set.Remove(entity);
}
