using System.Linq.Expressions;

namespace Service.Pattern;

/// <summary>
/// Generic service contract layered on top of a repository. Adapted from the SaaSify
/// Service.Pattern (EF6 + OData) to a framework-free .NET 9 contract.
/// </summary>
public interface IService<TEntity> where TEntity : class
{
    TEntity? Find(Func<TEntity, bool> match);
    IEnumerable<TEntity> GetAll();
    IEnumerable<TEntity> Where(Expression<Func<TEntity, bool>> predicate);
    void Insert(TEntity entity);
    void Update(TEntity entity);
    void Delete(TEntity entity);
}
