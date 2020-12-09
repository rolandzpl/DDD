using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// TODO: Replaying events (projections will need it on starting up of the system)
// TODO: MessageBus + EventPublisher that subscribes to new events and publishes to that bus

namespace DDD.Domain
{
    public class DefaultEventTypeResolver : IEventTypeResolver
    {
        public Type GetEventType(string eventName)
        {
            return AppDomain
                .CurrentDomain
                .GetAssemblies()
                .SelectMany(asm => GetTypesForAssembly(asm).Where(t => t.Name == eventName))
                .FirstOrDefault();
        }

        private static IEnumerable<Type> GetTypesForAssembly(Assembly asm)
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
    }
}
