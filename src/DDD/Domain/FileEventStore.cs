using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// TODO: Replaying events (projections will need it on starting up of the system)
// TODO: MessageBus + EventPublisher that subscribes to new events and publishes to that bus

namespace DDD.Domain
{
	public class FileEventStore : IEventStore
	{
		private readonly string rootDirectory;
		private readonly IFileSystem fs;
		private readonly IEventSerializer serializer;
		private readonly JsonSerializer eventSerializer;

		public event EventHandler<NewEventsEventArgs> NewEvents;

		public FileEventStore(string rootDirectory, IFileSystem fs, IEventSerializer serializer)
		{
			this.rootDirectory = rootDirectory ?? throw new ArgumentNullException(nameof(rootDirectory));
			this.fs = fs ?? throw new ArgumentNullException(nameof(fs));
			this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
			this.eventSerializer = JsonSerializer.CreateDefault();
		}

		public IEnumerable<Event> GetEventsById(object id)
		{
			return GetEventDataForId(id)
				.OrderBy(ed => ed.AggregateVersion)
				.Select(ed => DeserializePayload(ed))
				.ToList();
		}

		private Type GetEventType(string eventName)
		{
			return AppDomain
				.CurrentDomain
				.GetAssemblies()
				.SelectMany(asm => GetTypesForAssembly(asm).Where(t => t.Name == eventName))
				.FirstOrDefault();
		}

		private static IEnumerable<Type> GetTypesForAssembly(System.Reflection.Assembly asm)
		{
			try
			{
				return asm.GetTypes();
			}
			catch
			{
				return Enumerable.Empty<Type>();
			}
		}

		private IEnumerable<EventData> GetEventDataForId(object id)
		{
			var files = fs.GetFiles(rootDirectory, $"*#{id:N}#*.event");
			foreach (var f in files)
			{
				yield return LoadEvent(f);
			}
		}

		private EventData LoadEvent(string path)
		{
			using (var reader = fs.OpenText(path))
			using (var json = new JsonTextReader(reader))
			{
				return eventSerializer.Deserialize<EventData>(json);
			}
		}

		public void SaveEvents(object id, IEnumerable<Event> events, int expectedVersion)
		{
			if (expectedVersion <= GetMaxVersion(id))
			{
				throw new ConcurrencyException();
			}
			var currentVersion = expectedVersion;
			foreach (var e in events)
			{
				var timestamp = DateTime.UtcNow;
				var path = Path.Combine(rootDirectory, $"{timestamp.Ticks:X16}#{id:N}#{currentVersion}.event");
				using (TextWriter writer = fs.CreateText(path))
				{
					e.Version = currentVersion;
					eventSerializer.Serialize(writer, new EventData()
					{
						Timestamp = timestamp,
						EventId = Guid.NewGuid(),
						EventName = e.GetType().Name,
						AggregateId = id,
						AggregateVersion = currentVersion++,
						Payload = serializer.Serialize(e)
					});
					writer.Flush();
				}
			}
			NewEvents?.Invoke(this, new NewEventsEventArgs(events));
		}

		private int GetMaxVersion(object id)
		{
			return fs
				.GetFiles(rootDirectory, $"*#{id:N}#*.event")
				.Select(_ => Path.GetFileName(_).Split('#').ElementAt(2))
				.Select(_ => _.Replace(".event", string.Empty))
				.Select(_ => int.Parse(_))
				.DefaultIfEmpty(int.MinValue)
				.Max();
		}

		public IEnumerable<Guid> GetAllEvents()
		{
			return fs
				.GetFiles(rootDirectory, "*#*#*.event")
				.Select(f => new { Ticks = Path.GetFileName(f).Split('#').First(), Path = f })
				.OrderBy(_ => _.Ticks)
				.Select(_ => _.Path)
				.Select(_ => LoadEvent(_))
				.Select(_ => _.EventId);
		}

		private Event DeserializePayload(EventData ed)
		{
			return serializer.Deserialize(ed.Payload, GetEventType(ed.EventName));
		}

		public Event GetEvent(Guid eventId)
		{
			return fs
				.GetFiles(rootDirectory, "*#*#*.event")
				.Select(_ => LoadEvent(_))
				.Where(_ => _.EventId == eventId)
				.Select(_ => DeserializePayload(_))
				.SingleOrDefault();
		}
	}
}
