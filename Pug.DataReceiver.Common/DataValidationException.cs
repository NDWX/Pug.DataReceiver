using System;
using System.Linq;
using System.Runtime.Serialization;

namespace Pug.DataReceiver
{
	[Serializable]
	public class DataValidationException : Exception
	{
		private const string ValidationErrorsFieldName = "VALIDATION_ERRORS";

		public string[]? Errors { get; }

		public DataValidationException( string[] errors )
		{
			Errors = errors;
		}

		protected DataValidationException( SerializationInfo info, StreamingContext context )
			: base( info, context )
		{
			Errors = info?.GetString( ValidationErrorsFieldName )?.Split( ";" );
		}

		public override void GetObjectData( SerializationInfo info, StreamingContext context )
		{
			base.GetObjectData( info, context );

			info.AddValue( ValidationErrorsFieldName, Errors?.Aggregate( ( x, y ) => $"{x};{y}" ) );
		}
	}
}