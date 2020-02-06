using PersonalWebsite.BcBlog;
using PersonalWebsite.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PersonalWebsite.Models
{
    public class BlogPostViewModel
    {
        public BlogSeries Series { get; set; }
        public BlogPost   LastPost { get; set; }
        public BlogPost   CurrentPost { get; set; }
        public BlogPost   NextPost { get; set; }
    }
    
    public class BlogIndexViewModel
    {
        public IEnumerable<BlogSeriesAndPosts> Series { get; set; }
    }
}
