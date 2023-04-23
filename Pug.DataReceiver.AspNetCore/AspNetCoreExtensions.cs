
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace Pug.DataReceiver.Http.Components
{
	public static class AspNetCoreExtensions
	{
		internal static bool TryGetService<T>( this IServiceProvider serviceProvider, out T? service )
		{
			service = serviceProvider.GetService<T>();

			return service is not null;
		}

		private static IResult Handle( OneOf<Unit, DataFormatException, DataSubmissionException, Exception> result )
		{
			return result.When(
					unit => Results.Ok(),
					dataFormatException => Results.BadRequest( dataFormatException.Message ),
					dataSubmissionException => Results.BadRequest( dataSubmissionException.Message ),
					exception => Results.Problem( new ProblemDetails() { Status = 500 } )
				);
		}

		private static async Task<IDictionary<string, string>?> GetCredentialsAsync(
			HttpContext context, string authenticationHeader,
			Func<string, Task<IDictionary<string, string>>>? credentialsParser )
		{
			IDictionary<string, string>? credentials = null;

			if( !context.Request.Headers.TryGetValue( authenticationHeader, out StringValues credentialsStrings ) ) return null;

			if( credentialsParser is null && context.RequestServices.TryGetService( out ICredentialsParser? service ) )
				credentialsParser = service!.ParseAsync;

			if( credentialsParser is not null )
				credentials = await credentialsParser( credentialsStrings );

			return credentials;
		}

		public static RouteHandlerBuilder UseDataReceiver<T>(
			this WebApplication webApplication,
			string basePath, string authenticationHeader,
			Func<string, Task<IDictionary<string, string>>>? credentialsParser = null,
			Func<OneOf<Unit, DataFormatException, DataSubmissionException, Exception>, Task<IResult>>? resultHandler = null )
			where T : IDataReceiver
		{
			return webApplication.MapPost( $"/{basePath}/{{**path}}",
											async (string path, HttpContext context) =>
											{
												IDictionary<string, string>? credentials;

												credentials = await GetCredentialsAsync( context, authenticationHeader, credentialsParser );

												T? dataReceiver = context.RequestServices.GetService<T>();

												if( dataReceiver is null )
													return Results.Problem(
															new ProblemDetails()
															{
																Status = 500,
															}
														);

												OneOf<Unit, DataFormatException, DataSubmissionException, Exception> result =
													await dataReceiver?.SubmitAsync( credentials, path, context.Request.Body );

												if( resultHandler is null )
													return Handle( result );

												return await resultHandler( result );

											}
				);

		}
	}
}