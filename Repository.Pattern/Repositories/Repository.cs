using System.Linq.Expressions;

namespace Repository.Pattern.Repositories;

/// <summary>
/// In-memory implementation of <see cref="IRepository{TEntity}"/> backed by a <see cref="List{T}"/>.
/// Suitable for the JSON/in-memory persistence used by Marketplace Copilot.
/// </summary>
public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
{
    private readonly List<TEntity> _items;

    public Repository() => _items = [];

    public Repository(IEnumerable<TEntity> items) => _items = items.ToList();

    public TEntity? Find(Func<TEntity, bool> match) => _items.FirstOrDefault(match);

    public IEnumerable<TEntity> GetAll() => _items;

    public IEnumerable<TEntity> Where(Expression<Func<TEntity, bool>> predicate) =>
        _items.AsQueryable().Where(predicate);

    public IQueryable<TEntity> Queryable() => _items.AsQueryable();

    public void Insert(TEntity entity) => _items.Add(entity);

    public void InsertRange(IEnumerable<TEntity> entities) => _items.AddRange(entities);

    public void Update(TEntity entity)
    {
        // Reference-tracked in-memory entities are updated in place; no-op for parity with EF semantics.
    }

    public void Delete(TEntity entity) => _items.Remove(entity);
}
