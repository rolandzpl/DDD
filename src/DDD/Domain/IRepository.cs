namespace DDD.Domain
{
	public interface IRepository<TEntity, TId> where TEntity : AggregateRoot<TId>
	{
		TEntity GetItemById(TId id);

		void Save(TEntity item);
	}
}