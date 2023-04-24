
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

		private static IResult Handle( OneOf<Unit, Exception> result )
		{
			return result.When(
					unit => Results.Ok(),
					exception =>
						exception switch
						{
							UnsupportedDataTypeException => Results.StatusCode( 415 ),
							InvalidDataPathException => Results.NotFound(),
							DataFormatException e => Results.BadRequest( e.Message ),
							DataValidationException e => Results.ValidationProblem( new Dictionary<string, string[]>() {["VALIDATION_ERRORS"] = e.Errors} ),
							DataSubmissionException e => Results.BadRequest( e.Message ),
							_ => Results.Problem( )
						}
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
			Func<OneOf<Unit, Exception>, Task<IResult>>? resultHandler = null )
			where T : IDataReceiver
		{
			async Task<IResult> ReceiveData( string path, HttpContext context )
			{
				if( context.Request.ContentLength == 0 ) return Results.BadRequest();

				T? dataReceiver = context.RequestServices.GetService<T>();

				if( dataReceiver is null ) return Results.Problem( new ProblemDetails() { Status = 500 } );

				IDictionary<string, string>? credentials = await GetCredentialsAsync( context, authenticationHeader, credentialsParser );

				OneOf<Unit, Exception> result = await dataReceiver?.SubmitAsync( credentials, path, context.Request.Body, context.Request.ContentType )!;

				if( resultHandler is null ) return Handle( result );

				return await resultHandler( result );
			}

			return webApplication.MapPost( $"/{basePath}/{{**path}}", ReceiveData );
		}
	}
}