using Newtonsoft.Json;
using System;
using System.IO;

namespace DDD.Domain
{
	public class JsonEventSerializer : IEventSerializer
	{
		private readonly JsonSerializer serializer;

		public JsonEventSerializer()
		{
			this.serializer = JsonSerializer.CreateDefault();
		}

		public Event Deserialize(string s, Type type)
		{
			var reader = new StringReader(s);
			var json = new JsonTextReader(reader);
			return (Event)serializer.Deserialize(json, type);
		}

		public string Serialize(Event e)
		{
			var writer = new StringWriter();
			serializer.Serialize(writer, e);
			return writer.GetStringBuilder().ToString();
		}
	}
}