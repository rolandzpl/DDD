using System;
using System.Collections.Generic;
using System.Linq;

namespace DDD.Domain
{
	public class NewEventsEventArgs : EventArgs
	{
		public NewEventsEventArgs(IEnumerable<Event> events)
		{
			NewEvents = events.ToList();
		}

		public List<Event> NewEvents { get; }
	}
}
