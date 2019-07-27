using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PersonalWebsite.Models;

namespace PersonalWebsite.Data
{
    public class Projects
    {
        public static readonly List<Project> Store = new List<Project>
        {
            new Project
            {
                Name        = "FarmMaster",
                Description = "A website to help manage farms. Includes dynamic pages, custom role system, and more.",
                Tags        = new[] { "C#", "ASP", ".Net Core", "JS/TS", "CSS", "Website", "Server" },
                View        = "/Views/Projects/FarmMaster.cshtml",
                ImageUri    = "/img/farm_master_login.png"
            },
            new Project
            {
                Name        = "Computer Science Coursework - Tic-tac-toe AI",
                Description = "My coursework project for my A Level Computer Science course. An AI that uses basic machine learning to play Tic-Tac-Toe.",
                Tags        = new[] { "C#", "WPF", "AI", "Machine Learning" },
                View        = "/Views/Projects/CSProject.cshtml",
                ImageUri    = "/img/cs_project.png"
            }
        };
    }
}
