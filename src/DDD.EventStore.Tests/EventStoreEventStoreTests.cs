using DDD.Domain;
using EventStore.ClientAPI;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace DDD.EventStore
{
	[TestFixture]
	public partial class EventStoreEventStoreTests
	{
		[Test]
		[Order(1)]
		public void SaveEvents_NewInventoryCreated_EventsGetWrittenToStore()
		{
			var id = inventoryName;
			var target = new EventStoreEventStore(connection);
			target.SaveEvents(new Event<string>[]
			{
				Events.CreatedInventory(id),
				Events.ItemSuppliedToInventory(id, 0, "BC107", 10),
				Events.ItemWithdrawnFromInventory(id, 1, "BC107", 10),
			});
		}

		[Test]
		[Order(2)]
		public void SaveEvents_ItemAddedToInventory_EventsGetWrittenToStore()
		{
			var id = inventoryName;
			var target = new EventStoreEventStore(connection);
			target.SaveEvents(new Event<string>[]
			{
				Events.CreatedInventory(id),
				Events.ItemSuppliedToInventory(id, 0, "BC107", 10),
				Events.ItemWithdrawnFromInventory(id, 1, "BC107", 10),
			});
		}

		[SetUp]
		protected async Task SetUp()
		{
			connection = EventStoreConnection.Create(
				new Uri("tcp://admin:changeit@localhost:1113"), "ES-Connection");
			await connection.ConnectAsync();
		}

		[TearDown]
		protected async Task TearDown()
		{
			//await connection.DeleteStreamAsync(inventoryName, ExpectedVersion.Any);
		}

		private string inventoryName= "electronics-parts-inventory";
		private IEventStoreConnection connection;
	}
}