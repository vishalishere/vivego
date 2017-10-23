using System.Collections.Generic;
using System.Runtime.Serialization;

namespace vivego.WampSharp.Proto.SubPub.Backplane
{
	[DataContract(Name = "forwardedWampMessage")]
	public class ForwardedWampMessage
	{
		[DataMember(Name = "pid")]
		public string PublisherId { get; set; }

		[DataMember(Name = "topic")]
		public string WampTopic { get; set; }

		[DataMember(Name = "arg")]
		public object[] Arguments { get; set; }

		[DataMember(Name = "akw")]
		public IDictionary<string, object> ArgumentsKeywords { get; set; }
	}
}