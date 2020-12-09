using NUnit.Framework;
using System;
using System.Collections.Generic;

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
            var id = Guid.Empty;
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
            var repository = GetRepository(EmptyHistory);
            Assert.Throws<KeyNotFoundException>(() => repository.GetItemById(Guid.NewGuid()));
        }

        [Test]
        public void Save_GivenHistory_ReturnsInstantInFinalState()
        {
            var events = EmptyHistory;
            var repository = GetRepository(events);
            repository.Save(new TestDomain(Guid.Empty));
            Assert.That(events, Is.Not.Empty);
        }

        [Test]
        public void Save_ObjectAlreadyExistsInHistoryAndHasChanges_ChangesSavedToEventStore()
        {
            var domainObjectId = Guid.Empty;
            var events = EmptyHistory;
            events.Add(new TestDomainCreatedEvent(domainObjectId));
            var repository = GetRepository(events);
            var domainObject = repository.GetItemById(domainObjectId);
            var originalVersion = domainObject.Version;
            domainObject.DoSomeChanges();
            repository.Save(domainObject);
            Assert.Multiple(() =>
            {
                Assert.That(domainObject.Version, Is.GreaterThan(originalVersion));
                Assert.That(events.Count, Is.GreaterThan(1));
            });
        }

        [Test]
        public void Save_Successfull_UncommittedChangesAreEmpty()
        {
            var id = Guid.Empty;
            var obj = new TestDomain(id);
            var repository = GetRepository(EmptyHistory);
            repository.Save(obj);
            Assert.That(obj.GetUncommittedChanges(), Is.Empty);
        }

        [Test]
        public void Save_PersistanceError_AllChangesRemainInInstance()
        {
            var obj = new TestDomain(Guid.Empty);
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
