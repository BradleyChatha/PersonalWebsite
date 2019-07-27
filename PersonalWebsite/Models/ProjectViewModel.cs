using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PersonalWebsite.Models
{
    public class Project
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string View { get; set; }
        public string ImageUri { get; set; }
        public IEnumerable<string> Tags { get; set; }
    }

    public class ProjectFilterViewModel
    {
        public string Name { get; set; }
        public string Tags { get; set; }
    }

    public class ProjectContentViewModel
    {
        public IEnumerable<Project> Projects { get; set; }
    }

    public class ProjectViewModel
    {
        public ProjectContentViewModel Content { get; set; }
        public IEnumerable<string> AllTags { get; set; }
    }
}
