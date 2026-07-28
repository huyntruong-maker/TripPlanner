using Application.Interfaces.DataAccess;
using NSubstitute;

namespace Tests.TestSupport;

public static class UnitOfWorkFake
{
    public static IWriteUnitOfWork CreateWrite()
    {
        return Substitute.For<IWriteUnitOfWork>();
    }

    public static IReadUnitOfWork CreateRead()
    {
        return Substitute.For<IReadUnitOfWork>();
    }

    public static List<T> RegisterRepository<T>(this IWriteUnitOfWork writeUnitOfWork, List<T>? seed = null, Action? beforeRead = null)
        where T : class
    {
        var store = seed ?? [];
        var repo = GenericRepositoryFake.Create(store, beforeRead);
        writeUnitOfWork.GetRepository<T>().Returns(repo);
        return store;
    }

    public static List<T> RegisterRepository<T>(this IReadUnitOfWork readUnitOfWork, List<T>? seed = null, Action? beforeRead = null)
        where T : class
    {
        var store = seed ?? [];
        var repo = GenericRepositoryFake.Create(store, beforeRead);
        readUnitOfWork.GetRepository<T>().Returns(repo);
        return store;
    }
}
