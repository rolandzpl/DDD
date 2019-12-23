namespace DDD.EventStore
{
	internal class CreatedInventory : InventoryEvent
	{
		public CreatedInventory(string id) 
			: base(id, -1)
		{
		}
	}
}