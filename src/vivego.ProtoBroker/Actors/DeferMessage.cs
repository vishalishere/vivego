using System;

using Proto;

namespace vivego.ProtoBroker.Actors
{
	public class DeferMessage
	{
		public TimeSpan Defer { get; set; }
		public PID Pid { get; set; }
		public object Message { get; set; }
	}
}