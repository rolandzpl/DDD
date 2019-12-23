using System;

// TODO: Replaying events (projections will need it on starting up of the system)
// TODO: MessageBus + EventPublisher that subscribes to new events and publishes to that bus

namespace DDD.Domain
{
	public class EventData
	{
		public DateTime Timestamp;
		public Guid EventId;
		public object AggregateId;
		public int AggregateVersion;
		public string Payload;
		public string EventName;
	}
}
