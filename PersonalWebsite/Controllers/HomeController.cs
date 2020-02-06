using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PersonalWebsite.Models;
using PersonalWebsite.Services;

namespace PersonalWebsite.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Awards()
        {
            return View();
        }

        public IActionResult OldBlog(string post)
        {
            // Meh. Not a great idea, but to be honest I don't really need to care about a better
            // way for this website.
            return (post == null)
                    ? View()
                    : View("/Views/BlogPosts/" + post + ".cshtml");
        }

        public IActionResult Blog([FromServices] IBlogProvider blogs)
        {
            return View(new BlogIndexViewModel
            {
                Series = blogs.GetBlogSeries()
            });
        }

        [Route("/BlogPost/{seriesRef}/{postIndex}")]
        public IActionResult BlogPost(string seriesRef, int postIndex, [FromServices] IBlogProvider blogs)
        {
            var series = blogs.GetBlogSeries().FirstOrDefault(s => s.Series.Reference == seriesRef);
            if(series == null)
                return Redirect("Blog");

            BlogPost lastBlog    = null;
            BlogPost currentBlog = null;
            BlogPost nextBlog    = null;

            if(postIndex > 0 && series.Posts.Count > 1)
                lastBlog = series.Posts[postIndex - 1];

            if(postIndex <= series.Posts.Count)
                currentBlog = series.Posts[postIndex];
            else
                return Redirect("Blog");

            if(postIndex < series.Posts.Count - 1)
                nextBlog = series.Posts[postIndex + 1];

            return View(new BlogPostViewModel
            {
                Series      = series.Series,
                LastPost    = lastBlog,
                CurrentPost = currentBlog,
                NextPost    = nextBlog
            });
        }
    }
}