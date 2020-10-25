using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using PersonalWebsite.Services;

namespace PersonalWebsite.Middleware
{
    /// <summary>
    /// Routes a blog's specified SEO URL directly to the correct controller route.
    /// </summary>
    public class BlogRouterMiddleware
    {
        private readonly RequestDelegate _next;

        public BlogRouterMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext httpContext, IBlogProvider blogs)
        {
            var post = blogs.GetBlogPostFromSeoUrlOrNull(httpContext.Request.Path, out int postIndex);
            if(post != null)
                httpContext.Request.Path = $"/BlogPost/{post.Series.Reference}/{postIndex}";

            return _next(httpContext);
        }
    }

    public static class BlogRouterMiddlewareExtensions
    {
        public static IApplicationBuilder UseBlogRouterMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<BlogRouterMiddleware>();
        }
    }
}
