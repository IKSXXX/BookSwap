using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace BookSwap.Web.Mocks;

public class MockQueryable<T> : IQueryable<T>, IAsyncEnumerable<T>, IOrderedQueryable<T>
{
    private readonly IQueryable<T> _inner;
    public MockQueryable(IQueryable<T> inner) { _inner = inner; Provider = new MockAsyncQueryProvider(inner.Provider); }
    public Type ElementType => _inner.ElementType;
    public Expression Expression => _inner.Expression;
    public IQueryProvider Provider { get; }
    public IEnumerator<T> GetEnumerator() => _inner.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _inner.GetEnumerator();
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken ct = default) => new MockAsyncEnumerator<T>(_inner.GetEnumerator());
}

public class MockAsyncQueryProvider : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;
    public MockAsyncQueryProvider(IQueryProvider inner) => _inner = inner;
    public IQueryable CreateQuery(Expression expression) => _inner.CreateQuery(expression);
    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new MockQueryable<TElement>(_inner.CreateQuery<TElement>(expression));
    public object? Execute(Expression expression) => _inner.Execute(expression);
    public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);
    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken ct = default)
    {
        var result = _inner.Execute(expression);
        var resultType = typeof(TResult);
        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var valueType = resultType.GenericTypeArguments[0];
            var typedResult = Convert.ChangeType(result, valueType);
            return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(valueType)!.Invoke(null, [typedResult])!;
        }
        return (TResult)result!;
    }
}

public class MockAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;
    public MockAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;
    public T Current => _inner.Current;
    public ValueTask DisposeAsync() { _inner.Dispose(); return ValueTask.CompletedTask; }
    public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(_inner.MoveNext());
}
