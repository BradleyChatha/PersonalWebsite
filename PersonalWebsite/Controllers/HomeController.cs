using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

        public IActionResult Projects([FromQuery] string name, [FromQuery] string tags)
        {
            return View(new ProjectViewModel
            {
                Content = new ProjectContentViewModel{ Projects = this.GetFilteredProjects(name, tags) },
                AllTags = Data.Projects.Store.SelectMany(p => p.Tags).Distinct()
            });
        }

        private IEnumerable<Project> GetFilteredProjects(string name, string tags)
        {
            return Data.Projects.Store
                                .Where(p => (name == null) ? true : Regex.IsMatch(p.Name, name, RegexOptions.IgnoreCase))
                                .Where(p => (tags == null) ? true : tags.Split(',').All(t => p.Tags.Select(t2 => t2.ToLower()).Contains(t.ToLower())));
        }
    }
}