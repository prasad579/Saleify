using Repository.Pattern.Repositories;

namespace Repository.Pattern.UnitOfWork;

/// <summary>
/// Coordinates repositories and commits changes as a single transaction.
/// Mirrors the SaaSify UnitOfWork contract, simplified for the in-memory store.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    int SaveChanges();
    IRepository<TEntity> Repository<TEntity>() where TEntity : class;
}
