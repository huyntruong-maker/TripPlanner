using System.Linq.Expressions;
using Application.Interfaces.DataAccess;
using Microsoft.EntityFrameworkCore.Query;
using NSubstitute;

namespace Tests.TestSupport;

public static class GenericRepositoryFake
{
    public static IBaseWriteRepository<T> Create<T>(List<T> store, Action? beforeRead = null)
        where T : class
    {
        var repo = Substitute.For<IBaseWriteRepository<T>>();

        repo.Single(
                Arg.Any<Expression<Func<T, bool>>?>(),
                Arg.Any<Func<IQueryable<T>, IOrderedQueryable<T>>?>(),
                Arg.Any<Func<IQueryable<T>, IIncludableQueryable<T, object>>?>(),
                Arg.Any<bool>())
            .Returns(callInfo =>
            {
                beforeRead?.Invoke();
                var predicate = callInfo.ArgAt<Expression<Func<T, bool>>?>(0);
                var query = store.AsQueryable();
                if (predicate != null) query = query.Where(predicate);
                return Task.FromResult(query.FirstOrDefault());
            });

        repo.QueryCondition(Arg.Any<Expression<Func<T, bool>>>(), Arg.Any<bool>())
            .Returns(callInfo =>
            {
                beforeRead?.Invoke();
                var predicate = callInfo.ArgAt<Expression<Func<T, bool>>>(0);
                return Task.FromResult(store.AsQueryable().Where(predicate));
            });

        repo.Any(Arg.Any<Expression<Func<T, bool>>>())
            .Returns(callInfo =>
            {
                beforeRead?.Invoke();
                var predicate = callInfo.ArgAt<Expression<Func<T, bool>>>(0);
                return Task.FromResult(store.AsQueryable().Any(predicate));
            });

        repo.QueryAll(Arg.Any<bool>())
            .Returns(_ =>
            {
                beforeRead?.Invoke();
                return Task.FromResult(store.AsQueryable());
            });

        repo.Add(Arg.Any<T>())
            .Returns(Task.CompletedTask)
            .AndDoes(callInfo => store.Add(callInfo.Arg<T>()));

        repo.Add(Arg.Any<IEnumerable<T>>())
            .Returns(Task.CompletedTask)
            .AndDoes(callInfo => store.AddRange(callInfo.Arg<IEnumerable<T>>()));

        repo.Update(Arg.Any<T>()).Returns(Task.CompletedTask);
        repo.Update(Arg.Any<IEnumerable<T>>()).Returns(Task.CompletedTask);

        repo.Delete(Arg.Any<T>())
            .Returns(Task.CompletedTask)
            .AndDoes(callInfo => store.Remove(callInfo.Arg<T>()));

        repo.Delete(Arg.Any<IEnumerable<T>>())
            .Returns(Task.CompletedTask)
            .AndDoes(callInfo =>
            {
                foreach (var entity in callInfo.Arg<IEnumerable<T>>().ToList())
                    store.Remove(entity);
            });

        return repo;
    }
}
