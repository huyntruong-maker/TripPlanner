using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace Tests.TestSupport;

internal sealed class TestAsyncQueryProvider<TEntity>(IQueryProvider inner) : IAsyncQueryProvider
{
    public IQueryable CreateQuery(Expression expression) => new TestAsyncEnumerable<TEntity>(expression);

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new TestAsyncEnumerable<TElement>(expression);

    public object? Execute(Expression expression) => inner.Execute(expression);

    public TResult Execute<TResult>(Expression expression) => inner.Execute<TResult>(expression);

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        var expectedResultType = typeof(TResult).GetGenericArguments()[0];
        var executionResult = typeof(IQueryProvider)
            .GetMethod(nameof(IQueryProvider.Execute), 1, [typeof(Expression)])!
            .MakeGenericMethod(expectedResultType)
            .Invoke(this, [expression]);

        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(expectedResultType)
            .Invoke(null, [executionResult])!;
    }
}

internal sealed class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable)
    {
    }

    public TestAsyncEnumerable(Expression expression) : base(expression)
    {
    }

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
}

internal sealed class TestAsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
{
    public T Current => inner.Current;

    public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(inner.MoveNext());

    public ValueTask DisposeAsync()
    {
        inner.Dispose();
        return ValueTask.CompletedTask;
    }
}
