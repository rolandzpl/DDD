using System;
using System.Collections.Generic;

namespace DDD.Domain
{
	public interface IEventStore
	{
		void SaveEvents(object id, IEnumerable<Event> events, int expectedVersion);

		IEnumerable<Event> GetEventsById(object id);

		Event GetEvent(Guid eventId);

		IEnumerable<Guid> GetAllEvents();

		event EventHandler<NewEventsEventArgs> NewEvents;
	}
}
