using DDD.Fakes;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StringReader = System.IO.StringReader;

namespace DDD.Domain
{
    class FileEventStoreTests
    {
        class Creating
        {
            [Test]
            public void Publish_SubscriberSubscribedForEvent_SubscrberReceivesMessage()
            {
                int counter = 0;
                var bus = new MessageBus();
                bus.Subscribe<TestDomainCreatedEvent>(e =>
                {
                    counter++;
                });
                for (int i = 0; i < 100; ++i)
                {
                    bus.Publish(new TestDomainCreatedEvent(Guid.NewGuid()));
                }
                Assert.That(counter, Is.EqualTo(100));
            }

            [Test]
            public void CreateEventStore_NullFileSystem_ThrowsException()
            {
                Assert.Multiple(() =>
                {
                    Assert.Throws<ArgumentNullException>(() => new FileEventStore("", null, new JsonEventSerializer()));
                    Assert.Throws<ArgumentNullException>(() => new FileEventStore("", new FakeFileSystem(), null));
                });
            }
        }

        class SavingOrReading
        {
            [Test]
            public void GetEventsById_EventsForAggregateExist_ReturnsListOfEvents()
            {
                StoreEventDataInFilesystem(aggregateId, new EventData
                {
                    Timestamp = DateTime.Parse("2019-09-03").AddTicks(100),
                    EventName = EVT_TEST_DOMAIN_CREATED,
                    Payload = serializer.Serialize(
                        new TestDomainCreatedEvent(aggregateId) { Version = 0 })
                }); ;
                var events = eventStore.GetEventsById(aggregateId);
                Assert.That(events.ElementAt(0),
                    Is.InstanceOf<TestDomainCreatedEvent>().And
                        .Matches<TestDomainCreatedEvent>(_ =>
                            _.Id == aggregateId &&
                            _.Version == 0));
            }

            [Test]
            public void GetEventsById_EventsForAggregateIsUnrecognized_ThrowsUnknownEventTypeException()
            {
                StoreEventDataInFilesystem(aggregateId, new EventData
                {
                    Timestamp = DateTime.Parse("2019-09-03").AddTicks(100),
                    EventName = EVT_UNKNOWN_EVENT,
                    Payload = serializer.Serialize(
                        new TestDomainCreatedEvent(aggregateId) { Version = 0 })
                }); ;
                Assert.Throws<UnknownEventTypeException>(() => eventStore.GetEventsById(aggregateId));
            }

            [Test]
            public void GetEventsById_NoEvents_ReturnsEmptyList()
            {
                var events = eventStore.GetEventsById(aggregateId);
                Assert.That(events, Is.Empty);
            }

            [Test]
            public void SaveEvents_NoConcurrencyConflict_NewEventFileCreated()
            {
                var events = GetEvents();
                eventStore.SaveEvents(aggregateId, events, -1);
                Assert.That(fileSystem.Files.Count(), Is.EqualTo(1));
            }

            [Test]
            public void SaveEvents_NoConcurrencyConflict_EventRaised()
            {
                var receivedEvents = new List<Event>();
                eventStore.NewEvents += (o, e) => receivedEvents.AddRange(e.NewEvents);
                var events = GetEvents();
                eventStore.SaveEvents(aggregateId, events, -1);
                Assert.That(receivedEvents, Has.Exactly(1).InstanceOf<TestDomainCreatedEvent>());
            }

            [Test]
            public void SaveEvents_SeveralEvents_EveryNextEventDataHasIncrementedVersion()
            {
                var events = new Event[]
                {
                    new TestDomainCreatedEvent(aggregateId),
                    new TestDomainDataChangedEvent(aggregateId, "New data #1"),
                    new TestDomainDataChangedEvent(aggregateId, "New data #2"),
                    new TestDomainDataChangedEvent(aggregateId, "New data #3"),
                    new TestDomainDataChangedEvent(aggregateId, "New data #4"),
                };
                eventStore.SaveEvents(aggregateId, events, -1);
                Assert.That(
                    fileSystem.Files.Select(f => DeserializeEvent(f.GetStringBuilder())).Select(e => e.AggregateVersion),
                    Is.EquivalentTo(new[] { 0, 1, 2, 3, 4 }));
            }

            [Test]
            public void SaveEvents_SeveralEvents_EveryNextEventHasIncrementedVersion()
            {
                var events = new Event[]
                {
                    new TestDomainCreatedEvent(aggregateId),
                    new TestDomainDataChangedEvent(aggregateId, "New data #1"),
                    new TestDomainDataChangedEvent(aggregateId, "New data #2"),
                    new TestDomainDataChangedEvent(aggregateId, "New data #3"),
                    new TestDomainDataChangedEvent(aggregateId, "New data #4"),
                };
                eventStore.SaveEvents(aggregateId, events, -1);
                Assert.That(
                    fileSystem.Files
                        .Select(f => DeserializeEvent(f.GetStringBuilder()))
                        .Select(ed => serializer.Deserialize(ed.Payload, GetEventType(ed.EventName)))
                        .Select(e => e.Version),
                    Is.EquivalentTo(new[] { 0, 1, 2, 3, 4 }));
            }

            private Type GetEventType(string eventName)
            {
                return AppDomain
                    .CurrentDomain
                    .GetAssemblies()
                    .SelectMany(asm => GetTypesForAssembly(asm).Where(t => t.Name == eventName))
                    .FirstOrDefault();
            }

            private static IEnumerable<Type> GetTypesForAssembly(System.Reflection.Assembly asm)
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

            private Event[] GetEvents()
            {
                return new Event[]
                {
                    new TestDomainCreatedEvent(aggregateId)
                };
            }

            [Test]
            public void SaveEvents_NoConcurrencyConflict_EventDataStored()
            {
                var events = GetEvents();
                eventStore.SaveEvents(aggregateId, events, -1);
                Assert.That(
                    fileSystem.Files.Select(f => DeserializeEvent(f.GetStringBuilder())),
                    Has.One.Matches<EventData>(e =>
                        e.AggregateId.Equals(aggregateId.ToString()) &&
                        e.EventName == "TestDomainCreatedEvent"));
            }

            [Test]
            public void SaveEvents_EventWithHigherVersionAlreadyExists_ConcurrencyExceptionIsThrown()
            {
                StoreEventDataInFilesystem(EVT_TEST_DOMAIN_CREATED, DateTime.UtcNow.AddTicks(100), aggregateId, 0);
                var events = GetEvents();
                Assert.Throws<ConcurrencyException>(() => eventStore.SaveEvents(aggregateId, events, -1));
            }

            [Test]
            public void GetAllEvents_SomeEventsExistInTheSystem_ReturnsEventsOrderredByTimestamp()
            {
                var aggregateId1 = Guid.NewGuid();
                var aggregateId2 = Guid.NewGuid();
                var now = DateTime.UtcNow;
                var eventId1 = StoreEventDataInFilesystem(EVT_TEST_DOMAIN_CREATED, now.AddTicks(100), aggregateId1, 0);
                var eventId2 = StoreEventDataInFilesystem(EVT_TEST_DOMAIN_CREATED, now.AddTicks(101), aggregateId2, 0);
                var eventId3 = StoreEventDataInFilesystem(EVT_TEST_DOMAIN_CREATED, now.AddTicks(102), aggregateId1, 1);
                var eventIds = eventStore.GetAllEvents();
                Assert.Multiple(() =>
                {
                    Assert.That(eventIds.Count(), Is.EqualTo(3));
                    Assert.That(eventIds.ElementAt(0), Is.EqualTo(eventId1));
                    Assert.That(eventIds.ElementAt(1), Is.EqualTo(eventId2));
                    Assert.That(eventIds.ElementAt(2), Is.EqualTo(eventId3));
                });
            }

            [Test]
            public void GetEvent_EventExists_EventGetsReturned()
            {
                var now = DateTime.UtcNow;
                var eventId = StoreEventDataInFilesystem(EVT_TEST_DOMAIN_CREATED, now.AddTicks(100), aggregateId, 0);
                var e = eventStore.GetEvent(eventId);
                Assert.That(e, Is.Not.Null);
            }

            [Test]
            public void GetEvent_UnrecognizedEventExists_ThrowsUnknownEventTypeException()
            {
                var now = DateTime.UtcNow;
                var eventId = StoreEventDataInFilesystem(EVT_UNKNOWN_EVENT, now.AddTicks(100), aggregateId, 0);
                Assert.Throws<UnknownEventTypeException>(() => eventStore.GetEvent(eventId));
            }

            [Test]
            public void GetEvent_EventNotExists_NullGetsReturned()
            {
                var now = DateTime.UtcNow;
                StoreEventDataInFilesystem(EVT_TEST_DOMAIN_CREATED, now.AddTicks(100), aggregateId, 0);
                var e = eventStore.GetEvent(Guid.NewGuid());
                Assert.That(e, Is.Null);
            }

            private Guid StoreEventDataInFilesystem(string eventName, DateTime now, Guid aggregateId, int version)
            {
                var eventId = Guid.NewGuid();
                StoreEventDataInFilesystem(aggregateId,
                    new EventData()
                    {
                        Timestamp = now,
                        EventId = eventId,
                        EventName = eventName,
                        AggregateId = aggregateId,
                        AggregateVersion = version,
                        Payload = serializer.Serialize(new TestDomainCreatedEvent(aggregateId))
                    });
                return eventId;
            }

            private void StoreEventDataInFilesystem(Guid aggregateId, EventData eventData)
            {
                var file = fileSystem.CreateText($"\\{eventData.Timestamp.Ticks:X16}#{aggregateId:N}#{eventData.AggregateVersion}.event");
                var sss = JsonSerializer.CreateDefault();
                sss.Serialize(file, eventData);
            }

            private EventData DeserializeEvent(StringBuilder sb)
            {
                var serializer = JsonSerializer.CreateDefault();
                var reader = new StringReader(sb.ToString());
                var json = new JsonTextReader(reader);
                return serializer.Deserialize<EventData>(json);
            }

            [SetUp]
            protected void SetUp()
            {
                fileSystem = new FakeFileSystem();
                serializer = new JsonEventSerializer();
                eventStore = new FileEventStore("\\", fileSystem, serializer);
                aggregateId = Guid.NewGuid();
            }

            private FakeFileSystem fileSystem;
            private JsonEventSerializer serializer;
            private FileEventStore eventStore;
            private Guid aggregateId;
            public const string EVT_TEST_DOMAIN_CREATED = "TestDomainCreatedEvent";
            public const string EVT_UNKNOWN_EVENT = "UnknownEvent";
        }
    }
}