using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Pug.DataReceiver
{
	public interface IDataReceiver
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="credentials"></param>
		/// <param name="path"></param>
		/// <param name="data"></param>
		/// <param name="dataType"></param>
		/// <returns></returns>
		Task<OneOf<Unit, Exception>> SubmitAsync(
			IDictionary<string, string>? credentials, string path, Stream data, string? dataType );
	}
}