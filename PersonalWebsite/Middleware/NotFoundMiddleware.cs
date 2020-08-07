using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using PersonalWebsite.Constants;

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
                var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    new Uri($"mphp"                                 +
                            $"?idsite={MatomoConstants.SITE_ID}"    +
                            $"&rec=1"                               +
                            $"&uid=SERVER"                          +
                            $"&_id=0123456789ABCDEF"                +
                            $"&cid=0123456789ABCDEF"                +
                            $"&e_c=Error"                           +
                            $"&e_a=404"                             +
                            $"&e_n={httpContext.Request.Path}", 
                            UriKind.Relative
                    )
                );

                await client.SendAsync(request);

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
