using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Prometheus;

namespace PersonalWebsite.Middleware
{
    public class MetricsMiddleware
    {
        static readonly Counter _blogPostCounter = Metrics.CreateCounter(
            "post_viewed_total",
            "The amount of times a specific blog post has been viewed in total.",
            
            "seriesRef",
            "postIndex",
            "referrer"
        );

        private readonly RequestDelegate _next;

        public MetricsMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext httpContext)
        {
            var controller = (string)httpContext.Request.RouteValues["controller"];
            var action     = (string)httpContext.Request.RouteValues["action"];

            if(controller == "Home" && action == "BlogPost")
                this.OnBlogPostRequest(httpContext, controller, action);

            return _next(httpContext);
        }

        private void OnBlogPostRequest(HttpContext httpContext, string controller, string action)
        {
            var referrer  = httpContext.Request.GetTypedHeaders().Referer?.AbsolutePath ?? "Direct";
            var seriesRef = (string)httpContext.Request.RouteValues["seriesRef"];
            var postIndex = (string)httpContext.Request.RouteValues["postIndex"];

            _blogPostCounter
                .WithLabels(seriesRef, postIndex, referrer)
                .Inc();
        }
    }

    public static class MetricsMiddlewareExtensions
    {
        public static IApplicationBuilder UseMetricsMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<MetricsMiddleware>();
        }
    }
}
