using DDD.Domain;
using EventStore.ClientAPI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DDD.EventStore
{
	public class EventStoreEventStore : IEventStore
	{
		private readonly IEventStoreConnection connection;
		private readonly JsonSerializer serializer = JsonSerializer.CreateDefault();

		public EventStoreEventStore(IEventStoreConnection connection)
		{
			this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
		}

		public IEnumerable<Event> GetEventsById(object id)
		{
			throw new NotImplementedException();
		}

		public void SaveEvents(object id, IEnumerable<Event> events, int expectedVersion)
		{
			connection.AppendToStreamAsync(
					ResolveStreamName(events),
					events.Min(e => e.Version),
					events.Select(GetEventData))
				.Wait();
		}

		private static string ResolveStreamName(IEnumerable<Event> events)
		{
			return events
				.Select(e => e.Id)
				.Distinct()
				.Single()
				.ToString();
		}

		private EventData GetEventData<TId>(Event e)
		{
			return new EventData(Guid.NewGuid(), ResolveEventType(e), true, SerializeToBytes(e), null);
		}

		private static string ResolveEventType<TId>(Event<TId> e)
		{
			return e.GetType().Name;
		}

		private byte[] SerializeToBytes<TId>(Event<TId> e)
		{
			using (var stream = new MemoryStream())
			using (var writer = new StreamWriter(stream))
			using (var json = new JsonTextWriter(writer))
			{
				serializer.Serialize(json, e);
				json.Flush();
				return stream.GetBuffer();
			}
		}
	}
}
