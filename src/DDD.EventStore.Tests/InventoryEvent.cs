using DDD.Domain;

namespace DDD.EventStore
{
	public class InventoryEvent : Event<string>
	{
		public InventoryEvent(string id, int version) 
			: base(id, version)
		{
		}
	}
}