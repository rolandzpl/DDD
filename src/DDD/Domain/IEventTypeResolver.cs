using System;

// TODO: Replaying events (projections will need it on starting up of the system)
// TODO: MessageBus + EventPublisher that subscribes to new events and publishes to that bus

namespace DDD.Domain
{
    public interface IEventTypeResolver
    {
        Type GetEventType(string eventName);
    }
}
