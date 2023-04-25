using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Pug.DataReceiver
{
	public interface IDataReceiver
	{
		/// <summary>
		/// Submit <paramref name="data"/> of <paramref name="contentType"/> to <paramref name="path"/> using <paramref name="credentials"/>.
		/// </summary>
		/// <param name="credentials">Submitter credentials</param>
		/// <param name="path">The path to which data should be submitted</param>
		/// <param name="data">Data stream</param>
		/// <param name="contentType">Content type of data</param>
		/// <returns>
		/// <c>Unit</c> if data submission is successful, otherwise an Exception of one of the following sub types:
		/// <list type="bullet">
		///	<item><c>InvalidDataPathException</c>: Invalid <paramref name="path"/> specified</item>
		///	<item><c>UnsupportedDataTypeException</c>: >Specified <paramref name="contentType"/> is not supported</item>
		///	<item><c>DataFormatException</c>: Invalid <paramref name="data"/> format or structure</item>
		///	<item><c>DataValidationException</c>: Errors found during data validation</item>
		///	<item><c>DataSubmissionException</c>: Errors during data submission</item>
		/// </list>
		/// </returns>
		Task<OneOf<Unit, Exception>> SubmitAsync(
			IDictionary<string, string>? credentials, string? path, Stream data, string? contentType );
	}
}