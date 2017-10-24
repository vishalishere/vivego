using System.Globalization;
using System.IO;
using System.Threading;

using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

using vivego.Serializer.Abstractions;

namespace vivego.Serializer.NewtonSoft
{
	public class NewtonSoftJsonBinarySerializer : ISerializer<byte[]>
	{
		private readonly ThreadLocal<JsonSerializer> _jsonSerializer;

		public NewtonSoftJsonBinarySerializer() : this(GetDefaultJsonSerializerSettings())
		{
		}

		public NewtonSoftJsonBinarySerializer(JsonSerializerSettings jsonSerializerSettings)
		{
			_jsonSerializer = new ThreadLocal<JsonSerializer>(() => JsonSerializer.Create(jsonSerializerSettings));
		}

		public TType Deserialize<TType>(byte[] source)
		{
			if (source == null
				|| source.Length == 0)
			{
				return default;
			}

			using (MemoryStream memoryStream = new MemoryStream(source))
			using (BsonDataReader bsonDataReader = new BsonDataReader(memoryStream))
			{
				return _jsonSerializer.Value.Deserialize<TType>(bsonDataReader);
			}
		}

		public byte[] Serialize<TType>(TType type)
		{
			if (type == null)
			{
				return null;
			}

			using (MemoryStream memoryStream = new MemoryStream())
			using (BsonDataWriter bsonDataWriter = new BsonDataWriter(memoryStream))
			{
				_jsonSerializer.Value.Serialize(bsonDataWriter, type, typeof(TType));
				return memoryStream.ToArray();
			}
		}

		public static JsonSerializerSettings GetDefaultJsonSerializerSettings()
		{
			return new JsonSerializerSettings
			{
				TypeNameHandling = TypeNameHandling.All,
				PreserveReferencesHandling = PreserveReferencesHandling.Objects,
				NullValueHandling = NullValueHandling.Ignore,
				DateTimeZoneHandling = DateTimeZoneHandling.Utc,
				DateFormatHandling = DateFormatHandling.IsoDateFormat,
				Culture = CultureInfo.InvariantCulture,
				MissingMemberHandling = MissingMemberHandling.Ignore,
				DateParseHandling = DateParseHandling.DateTime,
				ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
				TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
			};
		}
	}
}