using System;
using System.Collections.Generic;

namespace DDD.Domain
{
	internal class DelegatedEventStore<TId> : IEventStore
	{
		private Func<object, IEnumerable<Event>> getEventsById;
		private Action<object, IEnumerable<Event>, int> persistEvents;

		public DelegatedEventStore(
			Func<object, IEnumerable<Event>> getEventsById,
			Action<object, IEnumerable<Event>, int> persistEvents)
		{
			this.getEventsById = getEventsById ?? throw new ArgumentNullException(nameof(getEventsById));
			this.persistEvents = persistEvents ?? throw new ArgumentNullException(nameof(persistEvents));
		}

		public event EventHandler<NewEventsEventArgs> NewEvents;

		public IEnumerable<Event> GetAllEvents()
		{
			throw new NotImplementedException();
		}

		public Event GetEvent(Guid eventId)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<Event> GetEventsById(object id)
		{
			return getEventsById(id);
		}

		public int SaveEvents(object id, IEnumerable<Event> events, int expectedVersion)
		{
			persistEvents(id, events, expectedVersion);
			NewEvents?.Invoke(this, new NewEventsEventArgs(events));
			return 0;
		}

		IEnumerable<Guid> IEventStore.GetAllEvents()
		{
			throw new NotImplementedException();
		}
	}
}