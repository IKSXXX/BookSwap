using System.Linq.Expressions;
using BookExchange.Db.Interfaces;

namespace BookExchange.Web.Mocks;

public class MockRepository<T> : IRepository<T> where T : class
{
    internal readonly List<T> Store;
    public MockRepository(List<T> store) => Store = store;

    public Task<T?> GetByIdAsync(object id)
    {
        var prop = typeof(T).GetProperty("Id");
        if (prop == null) return Task.FromResult<T?>(null);
        var item = Store.FirstOrDefault(x => Equals(prop.GetValue(x), id));
        return Task.FromResult(item);
    }

    public Task<IReadOnlyList<T>> GetAllAsync()
        => Task.FromResult<IReadOnlyList<T>>(Store.AsReadOnly());

    public IQueryable<T> Query() => new MockQueryable<T>(Store.AsQueryable());

    public Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate)
        => Task.FromResult<IReadOnlyList<T>>(Store.Where(predicate.Compile()).ToList().AsReadOnly());

    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        => Task.FromResult(Store.Any(predicate.Compile()));

    public Task AddAsync(T entity)
    {
        Store.Add(entity);
        return Task.CompletedTask;
    }

    public void Update(T entity)
    {
        var prop = typeof(T).GetProperty("Id");
        if (prop == null) return;
        var id = prop.GetValue(entity);
        var idx = Store.FindIndex(x => Equals(prop.GetValue(x), id));
        if (idx >= 0) Store[idx] = entity;
    }

    public void Remove(T entity) => Store.Remove(entity);
}
