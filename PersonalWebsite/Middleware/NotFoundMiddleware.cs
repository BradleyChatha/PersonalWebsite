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

            using var client = clientFactory.CreateClient(MatomoConstants.CLIENT_NAME);

            if (httpContext.Response.StatusCode == 404)
            {
                await client.PostMatomoEventAsync("Error", "404", httpContext.Request.Path);

                httpContext.Request.Path = "/";
                await this._next(httpContext);
            }
            else
            {
                // Thinking about playing around with Kubernetes soon, might use this as the test project, so knowing the node will help.
                //
                // If this adds too much overhead to initial page loads, then it might have to go though.
                await client.PostMatomoEventAsync(Environment.MachineName, "Served", httpContext.Request.Path);
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
