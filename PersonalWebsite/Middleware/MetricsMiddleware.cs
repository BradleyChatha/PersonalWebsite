using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace PersonalWebsite.Middleware
{
    public class MetricsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly InfluxDBClient _influx;

        public MetricsMiddleware(RequestDelegate next, InfluxDBClient influx)
        {
            _next = next;
            this._influx = influx;
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

            // TODO: Batch things
            using(var api = this._influx.GetWriteApi())
            {
                api.WritePoint(
                    "Personal", 
                    "Personal", 
                    PointData.Measurement("post_view")
                             .Tag("series_ref", seriesRef)
                             .Tag("post_index", postIndex)
                             .Field("referrer", referrer)
                             .Timestamp(DateTime.UtcNow, WritePrecision.Ns)
                );
            }
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
