using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

using Orleans.Providers.Streams.Common;
using Orleans.Serialization;
using Orleans.Streams;

namespace vivego.Orleans.Providers.Stream
{
	/// <summary>
	///     Represents the event sent and received from an In-Memory queue grain.
	/// </summary>
	[Serializable]
	[DataContract(Name = "messageData")]
	public class MessageData : IBatchContainer
	{
		public MessageData()
		{
		}

		public MessageData(SerializationManager serializationManager)
		{
			SerializationManager = serializationManager;
		}

		[IgnoreDataMember]
		internal SerializationManager SerializationManager { get; set; }

		/// <summary>
		///     Position of even in stream.
		/// </summary>
		[DataMember(Name = "seq", Order = 2)]
		public long SequenceNumber { get; set; }

		/// <summary>
		///     Serialized event data.
		/// </summary>
		[DataMember(Name = "data", Order = 3)]
		public byte[] Payload { get; set; }

		[DataMember(Name = "rctx", Order = 4)]
		public Dictionary<string, object> RequestContext { get; set; }

		public IEnumerable<Tuple<T, StreamSequenceToken>> GetEvents<T>()
		{
			T t = SerializationManager.DeserializeFromByteArray<T>(Payload);
			return new[]
			{
				new Tuple<T, StreamSequenceToken>(t, new EventSequenceTokenV2(SequenceNumber, 0))
			};
		}

		public bool ImportRequestContext()
		{
			return false;
		}

		//public bool ImportRequestContext()
		//{
		//	if (RequestContext != null
		//		&& RequestContext.Count > 0)
		//	{
		//		global::Orleans.Runtime.RequestContext..Import(RequestContext);
		//		return true;
		//	}

		//	return false;
		//}

		public bool ShouldDeliver(IStreamIdentity stream, object filterData, StreamFilterPredicate shouldReceiveFunc)
		{
			return true;
		}

		/// <summary>
		///     Stream Guid of the event data.
		/// </summary>
		[DataMember(Name = "strmId", Order = 0)]
		public Guid StreamGuid { get; set; }

		/// <summary>
		///     Stream namespace.
		/// </summary>
		[DataMember(Name = "strmNspc", Order = 1)]
		public string StreamNamespace { get; set; }

		[IgnoreDataMember]
		public StreamSequenceToken SequenceToken => new EventSequenceTokenV2(SequenceNumber);
	}
}