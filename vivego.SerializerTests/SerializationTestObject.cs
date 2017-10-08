using System;
using System.Runtime.Serialization;

namespace vivego.SerializerTests
{
	[DataContract(Name = "serializationTestObject")]
	public class SerializationTestObject
	{
		[DataMember(Name = "guid")]
		public Guid Guid { get; set; }

		[DataMember(Name = "string")]
		public string String { get; set; }

		[DataMember(Name = "float")]
		public float Float { get; set; }

		[DataMember(Name = "double")]
		public double Double { get; set; }

		[DataMember(Name = "int")]
		public int Int { get; set; }

		[DataMember(Name = "long")]
		public long Long { get; set; }

		[DataMember(Name = "dateTime")]
		public DateTime DateTime { get; set; }

		public static SerializationTestObject MakeInstance()
		{
			return new SerializationTestObject
			{
				Guid = Guid.NewGuid(),
				String = Guid.NewGuid().ToString(),
				Double = 123,
				Float = 123,
				Int = 123,
				Long = 123,
				DateTime = DateTime.UtcNow
			};
		}
	}
}