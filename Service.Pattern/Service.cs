using System.Linq.Expressions;
using Repository.Pattern.Repositories;

namespace Service.Pattern;

/// <summary>
/// Generic service base that delegates persistence to an <see cref="IRepository{TEntity}"/>.
/// Concrete domain services may inherit this to reuse standard CRUD plumbing.
/// </summary>
public abstract class Service<TEntity> : IService<TEntity> where TEntity : class
{
    private readonly IRepository<TEntity> _repository;

    protected Service(IRepository<TEntity> repository) => _repository = repository;

    public virtual TEntity? Find(Func<TEntity, bool> match) => _repository.Find(match);

    public virtual IEnumerable<TEntity> GetAll() => _repository.GetAll();

    public virtual IEnumerable<TEntity> Where(Expression<Func<TEntity, bool>> predicate) =>
        _repository.Where(predicate);

    public virtual void Insert(TEntity entity) => _repository.Insert(entity);

    public virtual void Update(TEntity entity) => _repository.Update(entity);

    public virtual void Delete(TEntity entity) => _repository.Delete(entity);
}
