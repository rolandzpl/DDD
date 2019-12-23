using System;
using DDD.Domain;

namespace DDD.EventStore
{
	class Events
	{
		public static ItemSuppliedToInventory ItemSuppliedToInventory(string id, int version, string symbol, uint amount)
		{
			return new ItemSuppliedToInventory(id, version)
			{
				Symbol = symbol,
				Amount = amount
			};
		}

		public static ItemWithdrawnFromInventory ItemWithdrawnFromInventory(string id, int version, string symbol, uint amount)
		{
			return new ItemWithdrawnFromInventory(id, version)
			{
				Symbol = symbol,
				Amount = amount
			};
		}

		internal static CreatedInventory CreatedInventory(string id)
		{
			return new CreatedInventory(id);
		}
	}
}
