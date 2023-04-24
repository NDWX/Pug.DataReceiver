using System;
using System.Runtime.Serialization;

namespace Pug.DataReceiver
{
	[Serializable]
	public class InvalidDataPathException : Exception
	{
		protected InvalidDataPathException( SerializationInfo info, StreamingContext context )
			: base( info, context )
		{
		}
	}
}