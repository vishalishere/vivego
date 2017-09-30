using vivego.core;

namespace vivego.PublishSubscribe.Serializer
{
	public class DefaultSerializer : ISerializer<byte[]>
	{
		public byte[] Serialize<TData>(TData t)
		{
			throw new System.NotImplementedException();
		}

		public TData Deserialize<TData>(byte[] serializedData)
		{
			throw new System.NotImplementedException();
		}
	}
}