using System;
using System.Runtime.Serialization;

namespace Pug.DataReceiver
{
	[Serializable]
	public class UnsupportedDataTypeException : Exception
	{
		protected UnsupportedDataTypeException( SerializationInfo info, StreamingContext context )
			: base( info, context )
		{
		}
	}
}