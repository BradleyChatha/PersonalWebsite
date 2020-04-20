using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace PersonalWebsite.Middleware
{
    public class RewriteMiddlewareConfig
    {
        public IDictionary<string, string> ExactRewrite { get; set; }

        public RewriteMiddlewareConfig()
        {
            this.ExactRewrite = new Dictionary<string, string>();
        }
    }

    public class RewriteMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RewriteMiddlewareConfig _config;

        public RewriteMiddleware(RequestDelegate next, RewriteMiddlewareConfig config)
        {
            _next = next;
            this._config = config;
        }

        public Task Invoke(HttpContext httpContext)
        {
            var pathAndQuery = httpContext.Request.Path + httpContext.Request.QueryString;

            var exactRewrite = this._config
                                   .ExactRewrite
                                   .Where(kvp => kvp.Key == pathAndQuery)
                                   .Select(kvp => kvp.Value)
                                   .FirstOrDefault();
            if(exactRewrite != null)
            {
                httpContext.Response.Redirect(exactRewrite, true);
                return Task.CompletedTask;
            }

            return _next(httpContext);
        }
    }

    public static class RewriteMiddlewareExtensions
    {
        public static IApplicationBuilder UseRewriteMiddleware(this IApplicationBuilder builder, Action<RewriteMiddlewareConfig> configurator)
        {
            var config = new RewriteMiddlewareConfig();
            configurator(config);
            return builder.UseMiddleware<RewriteMiddleware>(config);
        }
    }
}
