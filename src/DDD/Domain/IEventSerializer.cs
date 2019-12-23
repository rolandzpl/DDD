using System;

namespace DDD.Domain
{
	public interface IEventSerializer
	{
		string Serialize(Event e);

		Event Deserialize(string s, Type type);
	}
}