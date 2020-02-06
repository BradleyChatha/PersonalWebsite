using Markdig;
using Markdig.Syntax;
using Microsoft.AspNetCore.Hosting;
using PersonalWebsite.BcBlog;
using PersonalWebsite.MarkdigExtentions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PersonalWebsite.Services
{
    public sealed class BlogSeriesAndPosts
    {
        public BlogSeries Series { get; set; }
        public IList<BlogPost> Posts { get; set; }
    }

    public sealed class BlogPost
    {
        public string Title { get; set; }
        public DateTimeOffset DateCreated { get; set; }
        public DateTimeOffset DateUpdated { get; set; }
        public string GeneratedHtml { get; set; }
        public int OrderInSeries { get; set; }
    }

    public interface IBlogProvider
    {
        IEnumerable<BlogSeriesAndPosts> GetBlogSeries();
    }

    public class CachingBlogProvider : IBlogProvider
    {
        readonly IWebHostEnvironment _environment;
        IEnumerable<BlogSeriesAndPosts> _seriesCache;

        public CachingBlogProvider(IWebHostEnvironment environment)
        {
            this._environment = environment;
        }

        // I should technically make an options object, but there's literally no point in something so small-scale.
        // So the path to the series folder is just gonna be hard coded.
        public IEnumerable<BlogSeriesAndPosts> GetBlogSeries()
        {
            //if(this._seriesCache != null)
                //return this._seriesCache;

            var pipeline = new MarkdownPipelineBuilder()
                              .UseAutoLinks()
                              .UseAutoStdLink()
                              .UseBlogMetadata()
                              .Build();
            var order = 0;
            this._seriesCache = 
                this._environment.WebRootFileProvider
                                 .GetDirectoryContents("blogs/")
                                 .Where     (f => Path.GetExtension(f.Name) == ".bcm")
                                 .Select    (f => File.ReadAllText(f.PhysicalPath))
                                 .Select    (t => new Manifest(new ManifestParser(t)))
                                 .SelectMany(m => m.Series)
                                 .Select    (s => new BlogSeriesAndPosts
                                 {
                                     Series = s,
                                     Posts = s.PostFilePaths
                                              .Select(path => this._environment.WebRootPath + path)
                                              .Select(path => File.ReadAllText(path))
                                              .Select(text => 
                                              {
                                                  var document = Markdown.Parse(text, pipeline);
                                                  return new BlogPost 
                                                  {
                                                      GeneratedHtml = Markdown.ToHtml(text, pipeline), // meh
                                                      DateCreated   = this.FindRequiredMetadataAsDate(document, "date-created"),
                                                      DateUpdated   = this.FindRequiredMetadataAsDate(document, "date-updated"),
                                                      Title         = this.FindRequiredMetadataAsText(document, "title"),
                                                      OrderInSeries = order++
                                                  };
                                              }).ToList()
                                 })
                                 .ToList();
            return this._seriesCache;
        }

        private bool FindMetadata(MarkdownDocument document, string key, out string value)
        {
            var entry = document.Select(b => b as BlogMetadataBlock)
                                .Where(b => b != null)
                                .SelectMany(b => b.Entries)
                                .Where(b => b.Key == key)
                                .FirstOrDefault();
            if(entry.Key == null)
            {
                value = null;
                return false;
            }

            value = entry.Value;
            return true;
        }

        private DateTimeOffset FindRequiredMetadataAsDate(MarkdownDocument document, string key)
        {
            if (!this.FindMetadata(document, key, out string dateString))
                throw new InvalidDataException($"The blog post is missing the required metadata '@{key}'");

            return DateTimeOffset.Parse(dateString);
        }
        private string FindRequiredMetadataAsText(MarkdownDocument document, string key)
        {
            if (!this.FindMetadata(document, key, out string text))
                throw new InvalidDataException($"The blog post is missing the required metadata '@{key}'");

            return text;
        }
    }
}
