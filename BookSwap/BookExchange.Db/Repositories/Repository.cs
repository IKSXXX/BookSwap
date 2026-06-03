using System.Linq.Expressions;
using BookExchange.Db.Data;
using BookExchange.Db.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookExchange.Db.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly BookExchangeDbContext Context;
    protected readonly DbSet<T> DbSet;

    public Repository(BookExchangeDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(object id)
        => await DbSet.FindAsync(id);

    public async Task<IReadOnlyList<T>> GetAllAsync()
        => await DbSet.ToListAsync();

    public IQueryable<T> Query() => DbSet;

    public async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate)
        => await DbSet.Where(predicate).ToListAsync();

    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        => await DbSet.AnyAsync(predicate);

    public async Task AddAsync(T entity)
        => await DbSet.AddAsync(entity);

    public void Update(T entity)
        => DbSet.Update(entity);

    public void Remove(T entity)
        => DbSet.Remove(entity);
}
