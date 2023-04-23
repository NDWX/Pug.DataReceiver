using System;
using System.Runtime.Serialization;

namespace Pug.DataReceiver
{
	[Serializable]
	public class DataSubmissionException : Exception
	{
		protected DataSubmissionException( SerializationInfo info, StreamingContext context )
			: base( info, context )
		{
		}
	}
}