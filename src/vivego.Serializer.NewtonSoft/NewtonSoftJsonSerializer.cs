using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

using Newtonsoft.Json;

using vivego.Serializer.Abstractions;

namespace vivego.Serializer.NewtonSoft
{
	public class NewtonSoftJsonSerializer : ISerializer<string>
	{
		private readonly ThreadLocal<JsonSerializer> _jsonSerializer;

		public NewtonSoftJsonSerializer() : this(GetDefaultJsonSerializerSettings())
		{
		}

		public NewtonSoftJsonSerializer(JsonSerializerSettings jsonSerializerSettings)
		{
			_jsonSerializer = new ThreadLocal<JsonSerializer>(() => JsonSerializer.Create(jsonSerializerSettings));
		}

		public TType Deserialize<TType>(string source)
		{
			if (!string.IsNullOrEmpty(source))
			{
				return (TType) Deserialize(source, typeof(TType));
			}

			return default;
		}

		public string Serialize<TType>(TType type)
		{
			if (type == null)
			{
				return null;
			}

			StringWriter stringWriter = new StringWriter(new StringBuilder(256), CultureInfo.InvariantCulture);
			using (JsonTextWriter jsonTextWriter = new JsonTextWriter(stringWriter))
			{
				_jsonSerializer.Value.Serialize(jsonTextWriter, type, typeof(TType));
			}

			return stringWriter.ToString();
		}

		public object Deserialize(string source, Type type = null)
		{
			if (string.IsNullOrEmpty(source))
			{
				return null;
			}

			using (JsonTextReader jsonTextReader = new JsonTextReader(new StringReader(source)))
			{
				return type == null
					? _jsonSerializer.Value.Deserialize(jsonTextReader)
					: _jsonSerializer.Value.Deserialize(jsonTextReader, type);
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