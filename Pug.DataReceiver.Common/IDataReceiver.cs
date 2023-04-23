using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Threading.Tasks;

namespace Pug.DataReceiver
{
	public interface IDataReceiver
	{
		Task<OneOf<Unit, DataFormatException, DataSubmissionException, Exception>> SubmitAsync(
			IDictionary<string, string>? credentials, string path, Stream data );
	}
}