using System;

namespace DDD.Domain
{
	class TestDomainCreatedEvent : Event
	{
		public TestDomainCreatedEvent(Guid id)
		{
			Id = id;
			Version = -1;
		}

		public Guid Id { get; }
	}

	class TestDomainDataChangedEvent : Event
	{
		public TestDomainDataChangedEvent(Guid id, string data)
		{
			Id = id;
			Data = data;
		}

		public Guid Id { get; }

		public string Data { get; }
	}
}
