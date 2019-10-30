using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

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

        public IActionResult Blog(string post)
        {
            // Meh. Not a great idea, but to be honest I don't really need to care about a better
            // way for this website.
            return (post == null)
                    ? View()
                    : View("/Views/BlogPosts/" + post + ".cshtml");
        }
    }
}