using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PersonalWebsite.Models;

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

        public IActionResult Project([FromQuery] string view)
        {
            return View(view);
        }

        public IActionResult Projects()
        {
            return View(new ProjectViewModel
            {
                Content = new ProjectContentViewModel{ Projects = Data.Projects.Store },
                AllTags = Data.Projects.Store.SelectMany(p => p.Tags).Distinct()
            });
        }
    }
}