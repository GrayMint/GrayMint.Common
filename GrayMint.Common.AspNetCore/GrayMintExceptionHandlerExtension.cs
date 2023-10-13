﻿using System.Collections;
using System.Net;
using System.Net.Mime;
using System.Security.Authentication;
using System.Text.Json;
using GrayMint.Common.Client;
using GrayMint.Common.Exceptions;
using Microsoft.Extensions.Options;

namespace GrayMint.Common.AspNetCore;

public static class GrayMintExceptionHandlerExtension
{
    public class GrayMintExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly GrayMintExceptionHandlerOptions _grayMintExceptionOptions;
        private readonly ILogger<GrayMintExceptionMiddleware> _logger;

        public GrayMintExceptionMiddleware(RequestDelegate next, IOptions<GrayMintExceptionHandlerOptions> appExceptionOptions, ILogger<GrayMintExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
            _grayMintExceptionOptions = appExceptionOptions.Value;
        }

        private static Type GetExceptionType(Exception ex)
        {
            if (AlreadyExistsException.Is(ex)) return typeof(AlreadyExistsException);
            if (NotExistsException.Is(ex)) return typeof(NotExistsException);
            return ex.GetType();
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next.Invoke(context);
            }
            catch (Exception ex)
            {
                // set correct https status code depends on exception
                if (NotExistsException.Is(ex)) context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                else if (AlreadyExistsException.Is(ex)) context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                else if (ex is UnauthorizedAccessException || ex.InnerException is UnauthorizedAccessException) context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                else if (ex is AuthenticationException || ex.InnerException is AuthenticationException) context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                else context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

                // create typeFullName
                var typeFullName = GetExceptionType(ex).FullName;
                if (!string.IsNullOrEmpty(_grayMintExceptionOptions.RootNamespace))
                    typeFullName = typeFullName?.Replace(nameof(GrayMint), _grayMintExceptionOptions.RootNamespace);

                var message = ex.Message;
                if (!string.IsNullOrEmpty(ex.InnerException?.Message))
                    message += $" InnerMessage: {ex.InnerException?.Message}";

                // set optional information
                context.Response.ContentType = MediaTypeNames.Application.Json;
                var error = new ApiException.ServerException
                {
                    Data = new Dictionary<string, string?>(),
                    TypeName = GetExceptionType(ex).Name,
                    TypeFullName = typeFullName,
                    Message = message
                };

                foreach (DictionaryEntry item in ex.Data)
                {
                    var key = item.Key.ToString();
                    if (key != null)
                        error.Data.Add(key, item.Value?.ToString());
                }

                var errorJson = JsonSerializer.Serialize(error);
                await context.Response.WriteAsync(errorJson);

                _logger.LogError(ex, "{Message}. ErrorInfo: {ErrorInfo}", ex.Message , errorJson);
            }
        }
    }

    public static IApplicationBuilder UseGrayMintExceptionHandler(this IApplicationBuilder app, GrayMintExceptionHandlerOptions? appExceptionOptions = null)
    {
        appExceptionOptions ??= new GrayMintExceptionHandlerOptions();
        app.UseMiddleware<GrayMintExceptionMiddleware>(Options.Create(appExceptionOptions));
        return app;
    }
}