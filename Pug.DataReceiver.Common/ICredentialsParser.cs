using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pug.DataReceiver
{
	public interface ICredentialsParser
	{
		Task<IDictionary<string, string>?> ParseAsync( string? credentials );
	}
}