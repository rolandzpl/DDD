using DDD.Domain;
using System;

namespace DDD.EventStore
{
	public class ItemWithdrawnFromInventory : InventoryEvent
	{
		public ItemWithdrawnFromInventory(string id, int version) 
			: base(id, version)
		{
		}

		public string Symbol { get; set; }

		public uint Amount { get; set; }
	}
}