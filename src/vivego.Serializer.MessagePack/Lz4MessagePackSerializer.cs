using vivego.Serializer.Abstractions;

namespace vivego.Serializer.MessagePack
{
	public class Lz4MessagePackSerializer : ISerializer<byte[]>
	{
		public TType Deserialize<TType>(byte[] source)
		{
			if (source == null
				|| source.Length == 0)
			{
				return default(TType);
			}

			return global::MessagePack.LZ4MessagePackSerializer.Deserialize<TType>(source);
		}

		public byte[] Serialize<TType>(TType source)
		{
			return global::MessagePack.LZ4MessagePackSerializer.Serialize(source);
		}
	}
}