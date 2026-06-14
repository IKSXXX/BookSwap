using System.Linq.Expressions;
using BookSwap.Db.Interfaces;

namespace BookSwap.Web.Mocks;

public class MockRepository<T> : IRepository<T> where T : class
{
    internal readonly List<T> Store;
    public MockRepository(List<T> store) => Store = store;

    public Task<T?> GetByIdAsync(object id)
    {
        var idProperty = typeof(T).GetProperty("Id");
        if (idProperty == null) return Task.FromResult<T?>(null);
        var item = Store.FirstOrDefault(x => Equals(idProperty.GetValue(x), id));
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
        var idProperty = typeof(T).GetProperty("Id");
        if (idProperty == null) return;
        var id = idProperty.GetValue(entity);
        var index = Store.FindIndex(x => Equals(idProperty.GetValue(x), id));
        if (index >= 0) Store[index] = entity;
    }

    public void Remove(T entity) => Store.Remove(entity);
}
