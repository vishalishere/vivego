using vivego.Serializer.Abstractions;

namespace vivego.Serializer.MessagePack
{
	public class MessagePackSerializer : ISerializer<byte[]>
	{
		public TType Deserialize<TType>(byte[] source)
		{
			if (source == null
				|| source.Length == 0)
			{
				return default(TType);
			}

			return global::MessagePack.MessagePackSerializer.Deserialize<TType>(source);
		}

		public byte[] Serialize<TType>(TType type)
		{
			return global::MessagePack.MessagePackSerializer.Serialize(type);
		}
	}
}