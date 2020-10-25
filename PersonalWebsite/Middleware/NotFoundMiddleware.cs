using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using PersonalWebsite.Constants;
using PersonalWebsite.Extensions;

namespace PersonalWebsite.Middleware
{
    public class NotFoundMiddleware
    {
        private readonly RequestDelegate _next;

        public NotFoundMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext, IHttpClientFactory clientFactory)
        {
            await this._next(httpContext);

            if(httpContext.Response.StatusCode == 404)
            {
                using var client = clientFactory.CreateClient(MatomoConstants.CLIENT_NAME);
                await client.PostMatomoEventAsync(httpContext, "Error", "404");

                httpContext.Request.Path = "/";
                await this._next(httpContext);
            }
        }
    }

    public static class NotFoundMiddlewareExtensions
    {
        public static IApplicationBuilder UseNotFoundMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<NotFoundMiddleware>();
        }
    }
}
