using System;
using System.Runtime.Serialization;

namespace Pug.DataReceiver
{
	[Serializable]
	public class DataFormatException : Exception
	{
		protected DataFormatException( SerializationInfo info, StreamingContext context )
			: base( info, context )
		{
		}
	}
}