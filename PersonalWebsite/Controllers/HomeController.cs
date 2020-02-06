using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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
            // TEMP: Just so I can start testing now instead of later :p
            return View("Blog",
                blogs.GetBlogSeries()
                     .SelectMany(bs => bs.Posts)
                     .First()
                     .GeneratedHtml
            );
        }
    }
}