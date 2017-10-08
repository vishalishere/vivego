using System.IO;

using vivego.Serializer.Abstractions;

namespace vivego.Serializer.Wire
{
	public class WireSerializer : ISerializer<byte[]>
	{
		private readonly global::Wire.Serializer _serializer;

		public WireSerializer()
		{
			_serializer = new global::Wire.Serializer();
		}

		public WireSerializer(global::Wire.Serializer serializer)
		{
			_serializer = serializer;
		}

		public byte[] Serialize<TData>(TData t)
		{
			using (MemoryStream stream = new MemoryStream())
			{
				_serializer.Serialize(t, stream);
				return stream.ToArray();
			}
		}

		public TData Deserialize<TData>(byte[] serializedData)
		{
			using (MemoryStream stream = new MemoryStream(serializedData))
			{
				return _serializer.Deserialize<TData>(stream);
			}
		}
	}
}