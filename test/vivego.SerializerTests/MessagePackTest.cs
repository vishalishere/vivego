using BenchmarkDotNet.Attributes;

using vivego.Serializer.Abstractions;
using vivego.Serializer.MessagePack;
using vivego.Serializer.Wire;

namespace vivego.SerializerTests
{
	public class MessagePackTest
	{
		private readonly byte[] _deserializationTestObject;
		private readonly SerializationTestObject _serializationTestObject = SerializationTestObject.MakeInstance();
		private readonly ISerializer<byte[]> _serializer = new MessagePackSerializer();

		public MessagePackTest()
		{
			_deserializationTestObject = _serializer.Serialize(_serializationTestObject);
		}

		[Benchmark]
		public void SerializeSingle()
		{
			_serializer.Serialize(_serializationTestObject);
		}

		[Benchmark]
		public void DeserializeSingle()
		{
			_serializer.Deserialize<SerializationTestObject>(_deserializationTestObject);
		}
	}
}