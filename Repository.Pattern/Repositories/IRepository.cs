using System.Linq.Expressions;

namespace Repository.Pattern.Repositories;

/// <summary>
/// Generic repository abstraction over a single entity set. Adapted from the SaaSify
/// Repository.Pattern (EF6 + OData) to a framework-free .NET 9 contract.
/// </summary>
public interface IRepository<TEntity> where TEntity : class
{
    TEntity? Find(Func<TEntity, bool> match);
    IEnumerable<TEntity> GetAll();
    IEnumerable<TEntity> Where(Expression<Func<TEntity, bool>> predicate);
    IQueryable<TEntity> Queryable();
    void Insert(TEntity entity);
    void InsertRange(IEnumerable<TEntity> entities);
    void Update(TEntity entity);
    void Delete(TEntity entity);
}
