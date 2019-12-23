using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DDD.Domain
{
	class RepositoryTests
	{
		[Test]
		public void CreateRepository_NullEventStore_ThrowsException()
		{
			Assert.Throws<ArgumentNullException>(() => new Repository<TestDomain, Guid>(null));
		}

		[Test]
		public void GetItemById_GivenHistory_ReturnsInstantInFinalState()
		{
			var id = Guid.NewGuid();
			var history = new Event[]
			{
				new TestDomainCreatedEvent(id),
			};
			var repository = GetRepository(history);
			var obj = repository.GetItemById(id);
			Assert.That(obj, Is.Not.Null);
		}

		[Test]
		public void GetItemById_NotExistInGivenHistory_ThrowsException()
		{
			var repository = GetRepository(Enumerable.Empty<Event>().ToArray());
			Assert.Throws<KeyNotFoundException>(() => repository.GetItemById(Guid.NewGuid()));
		}

		[Test]
		public void Save_GivenHistory_ReturnsInstantInFinalState()
		{
			var savedEvents = EmptyHistory;
			var repository = GetRepository(savedEvents);
			repository.Save(new TestDomain(Guid.Empty));
			Assert.That(savedEvents, Is.Not.Empty);
		}

		[Test]
		public void Save_Successfull_UncommittedChangesAreEmpty()
		{
			var id = Guid.NewGuid();
			var obj = new TestDomain(id);
			var repository = GetRepository(EmptyHistory);
			repository.Save(obj);
			Assert.That(obj.GetUncommittedChanges(), Is.Empty);
		}

		[Test]
		public void Save_PersistanceError_AllChangesRemainInInstance()
		{
			var obj = new TestDomain(Guid.NewGuid());
			var repository = GetRepositoryThrowingError<Exception>(EmptyHistory);
			try { repository.Save(obj); } catch { }
			Assert.That(obj.GetUncommittedChanges(), Is.Not.Empty);
		}

		protected ICollection<Event> EmptyHistory
		{
			get { return new List<Event>(); }
		}

		protected Repository<TestDomain, Guid> GetRepository(ICollection<Event> events)
		{
			return new Repository<TestDomain, Guid>(
				new DelegatedEventStore<Guid>(_ => events, (id, e, expectedVersion) => PersistEvents(events, e)));
		}

		protected Repository<TestDomain, Guid> GetRepositoryThrowingError<TError>(ICollection<Event> events)
			where TError : Exception, new()
		{
			return new Repository<TestDomain, Guid>(
				new DelegatedEventStore<Guid>(_ => events, (id, e, expectedVersion) => throw new TError()));
		}

		private static void PersistEvents(ICollection<Event> storage, IEnumerable<Event> eventsToPersist)
		{
			foreach (var e in eventsToPersist)
			{
				storage.Add(e);
			}
		}
	}
}
