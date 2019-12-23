using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DDD.Domain
{
	public class Repository<TEntity, TId> where TEntity :
		AggregateRoot<TId>,
		IRepository<TEntity, TId>
	{
		private readonly IEventStore eventStore;

		public Repository(IEventStore eventStore)
		{
			this.eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
		}

		public TEntity GetItemById(TId id)
		{
			var history = eventStore.GetEventsById(id);
			if (!history.Any())
			{
				throw new KeyNotFoundException($"Instance with id {id} was not found");
			}
			var ctor = GetConstructor();
			var instance = (TEntity)ctor.Invoke(null);
			instance.LoadFromHistory(history);
			return instance;
		}

		private static ConstructorInfo GetConstructor()
		{
			return typeof(TEntity)
				.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
				.Where(c => c.GetParameters().Length == 0)
				.FirstOrDefault();
		}

		public void Save(TEntity item)
		{
			eventStore.SaveEvents(item.Id, item.GetUncommittedChanges(), item.Version);
			item.ClearUncommittedChanges();
		}
	}
}
