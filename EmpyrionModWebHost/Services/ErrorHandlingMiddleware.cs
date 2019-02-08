using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EmpyrionModWebHost.Services
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ErrorHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, ILogger<ErrorHandlingMiddleware> _logger)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                if (ex is InvalidOperationException && ex.Message.StartsWith("The SPA default page middleware could not return the default page"))
                {

                }
                else
                {
                    await HandleExceptionAsync(context, ex, _logger);
                }
            }
        }

        private static Task HandleExceptionAsync(
            HttpContext context,
            Exception exception,
            ILogger<ErrorHandlingMiddleware> _logger)
        {
            var code = HttpStatusCode.InternalServerError; // 500 if unexpected
            _logger.LogError("Unhandled excetion. {0}", exception);
            var result = JsonConvert.SerializeObject(
                new ErrorResponse
                {
                    Error = exception.ToString(),
                    ErrorDescription = exception.Message
                });
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)code;
            return context.Response.WriteAsync(result);
        }

        private class ErrorResponse
        {
            public string Error { get; set; }
            public string ErrorDescription { get; set; }
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class ErrorHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseErrorHandlingMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ErrorHandlingMiddleware>();
        }
    }
}
