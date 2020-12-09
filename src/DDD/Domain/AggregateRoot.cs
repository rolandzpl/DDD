using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DDD.Domain
{
	public abstract class AggregateRoot<TId>
	{
        public const int UNSPECIFIED_AGGREGATE_VERSION = -1;

        private readonly List<Event> changes = new List<Event>();

		public TId Id { get; protected set; }

		public int Version { get; internal set; } = UNSPECIFIED_AGGREGATE_VERSION;

		public void LoadFromHistory(IEnumerable<Event> history)
		{
			LoadFromHistoryImpl(GetOrderred(history));
		}

		private static IOrderedEnumerable<Event> GetOrderred(IEnumerable<Event> history)
		{
			return history.OrderBy(e => e.Version);
		}

		private void LoadFromHistoryImpl(IOrderedEnumerable<Event> orderredHistory)
		{
			foreach (var e in orderredHistory)
			{
				ApplyEvent(e);
				Version = e.Version;
			}
		}

		protected void ApplyNewEvent(Event e)
		{
			changes.Add(e);
			ApplyEvent(e);
		}

		private void ApplyEvent(Event e)
		{
			var handler = GetType()
				.GetRuntimeMethods()
				.Where(mi => mi.IsPrivate)
				.Where(mi => mi.Name == "Apply")
				.Where(mi => mi.GetParameters().Length == 1)
				.SingleOrDefault(mi => mi.GetParameters().SingleOrDefault()?.ParameterType == e.GetType());
			handler?.Invoke(this, new[] { e });
		}

		public IEnumerable<Event> GetUncommittedChanges()
		{
			return changes.ToList();
		}

		public void ClearUncommittedChanges()
		{
			changes.Clear();
		}
	}
}