
using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
							InvalidCredentialException => Results.Unauthorized(),
							InvalidDataPathException => Results.NotFound(),
							DataFormatException e => Results.BadRequest( e.Message ),
							DataValidationException e => Results.ValidationProblem( new Dictionary<string, string[]>() {["VALIDATION_ERRORS"] = e.Errors} ),
							DataSubmissionException e => Results.BadRequest( e.Message ),
							_ => throw exception
						}
				);
		}

		private static async Task<IDictionary<string, string>?> GetCredentialsAsync(
			HttpContext context, string authenticationHeaderName,
			Func<string, Task<IDictionary<string, string>?>>? credentialsParser, ILogger? logger )
		{
			IDictionary<string, string>? credentials = null;

			if( !context.Request.Headers.TryGetValue( authenticationHeaderName, out StringValues credentialsStrings ) )
			{
				logger?.LogInformation( "Authentication header {AuthenticationHeaderName} not specified in request", authenticationHeaderName );
				return null;
			}
			
			logger?.LogDebug( "Authentication header {AuthenticationHeaderName}: {AuthenticationHeaderValue}", 
							authenticationHeaderName, credentialsStrings.ToString() );

			if( credentialsParser is null && context.RequestServices.TryGetService( out ICredentialsParser? service ) )
				credentialsParser = service!.ParseAsync;

			if( credentialsParser is not null )
				credentials = await credentialsParser( credentialsStrings );

			return credentials;
		}

		/// <summary>
		/// Register data receiver with specified <paramref name="basePath"/>.
		/// </summary>
		/// <param name="webApplication">Instance of <c>WebApplication</c></param>
		/// <param name="basePath">Base path on which data receiver endpoint should be registered and may be more one or more levels relative to root path</param>
		/// <param name="allowSubPath">Whether data may be submitted to sub-path under <paramref name="basePath"/></param>
		/// <param name="authenticationHeader">Name of request header from which credentials should be retrieved</param>
		/// <param name="credentialsParser">Optional credentials parser</param>
		/// <param name="resultHandler">Optional <c>IDataReceiver.SubmitAsync</c> result to <c>IResult</c> mapping.</param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static RouteHandlerBuilder UseDataReceiver<T>(
			this WebApplication webApplication,
			string basePath = "/", bool allowSubPath = false, string? authenticationHeader = null,
			Func<string, Task<IDictionary<string, string>?>>? credentialsParser = null,
			Func<OneOf<Unit, Exception>, Task<IResult>>? resultHandler = null )
			where T : IDataReceiver
		{
			async Task<IResult> ReceiveData( string? path, HttpContext context )
			{
				ILoggerFactory? loggerFactory = context.RequestServices.GetService<ILoggerFactory>();
				ILogger? logger = loggerFactory?.CreateLogger( $"IDataReceiver/{basePath}" );

				if( !allowSubPath && !string.IsNullOrWhiteSpace( path ) )
				{
					logger?.LogInformation( "Sub-path '{SubPath}' is not allowed", path );
					return Results.NotFound();
				}

				if( context.Request.ContentLength == 0 ) return Results.BadRequest();

				T? dataReceiver = context.RequestServices.GetService<T>();

				if( dataReceiver is null )
				{
					logger?.LogError( "Data receiver '{DataReceiver}' not found", typeof(T).FullName );
					return Results.Problem( new ProblemDetails() { Status = 500 } );
				}

				IDictionary<string, string>? credentials = string.IsNullOrWhiteSpace(authenticationHeader)?
																null :
																await GetCredentialsAsync( context, authenticationHeader, credentialsParser, logger );

				OneOf<Unit, Exception> result = await dataReceiver.SubmitAsync( credentials, path, context.Request.Body, context.Request.ContentType )!;

				if( resultHandler is null ) return Handle( result );

				return await resultHandler( result );
			}

			string route = basePath.TrimEnd( '/' );
			route = route.StartsWith('/')? route : $"/{route}";

			route = $"{route}/{{**path}}";
			
			return webApplication.MapPost( route, ReceiveData );
		}
	}
}