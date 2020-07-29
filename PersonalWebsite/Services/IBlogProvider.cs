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

        public Uri GetPostSeoPath(int postIndex)
        {
            var post   = this.Posts[postIndex];
            var values = new string[]
            {
                Convert.ToString(post.OrderInSeries),
                post.SeoTag,
                this.Series.Tags.Aggregate((a,b) => a+'-'+b)
            };
            return new Uri($"{this.Series.Reference}/{values.Aggregate((a,b) => a+'-'+b)}", UriKind.Relative);
        }

        public Uri GetPostSeoPath(BlogPost p)
        {
            for(int i = 0; i < this.Posts.Count; i++)
            {
                if(this.Posts[i] == p)
                    return this.GetPostSeoPath(i);
            }

            throw new KeyNotFoundException();
        }
    }

    public sealed class BlogPost
    {
        public string           Title               { get; set; }
        public string           SeoTitle            { get; set; }
        public string           SeoTag              { get; set; }
        public DateTimeOffset   DateCreated         { get; set; }
        public DateTimeOffset   DateUpdated         { get; set; }
        public string           GeneratedHtml       { get; set; }
        public int              OrderInSeries       { get; set; }
        public string           GithubUrl           { get; set; }
        public string           CardImageGeneric    { get; set; }
        public string           CardImageTwitter    { get; set; }
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
            #if !DEBUG
            if(this._seriesCache != null)
                return this._seriesCache;
            #endif

            var pipeline = new MarkdownPipelineBuilder()
                              .UseAutoLinks()
                              .UseAutoStdLink()
                              .UseAutoIdentifiers()
                              .UseBlogMetadata()
                              .UseEmphasisExtras()
                              .Build();

            this._seriesCache = 
                this._environment.WebRootFileProvider
                                 .GetDirectoryContents("blogs/")
                                 .Where     (f => Path.GetExtension(f.Name) == ".bcm")
                                 .Select    (f => File.ReadAllText(f.PhysicalPath))
                                 .Select    (t => new Manifest(new ManifestParser(t)))
                                 .SelectMany(m => m.Series)
                                 .Select    (s => 
                                 {
                                     var order = 0;
                                     return new BlogSeriesAndPosts
                                     {
                                         Series = s,
                                         Posts = s.PostFilePaths
                                                  .Select(relativePath => 
                                                  {
                                                      var path     = this._environment.WebRootPath + relativePath;
                                                      var text     = File.ReadAllText(path);
                                                      var document = Markdown.Parse(text, pipeline);
                                                      return new BlogPost 
                                                      {
                                                          GeneratedHtml     = Markdown.ToHtml(text, pipeline), // meh
                                                          DateCreated       = this.FindRequiredMetadataAsDate(document, "date-created"),
                                                          DateUpdated       = this.FindRequiredMetadataAsDate(document, "date-updated"),
                                                          Title             = this.FindRequiredMetadataAsText(document, "title"),
                                                          SeoTitle          = this.FindFirstMatchingMetadataAsText(document, "seo-title", "title"),
                                                          SeoTag            = this.FindMetadataAsText(document, "seo-tag"),
                                                          CardImageGeneric  = this.FindMetadataAsText(document, "card-image"),
                                                          CardImageTwitter  = this.FindMetadataAsText(document, "card-image-twitter"),
                                                          GithubUrl         = $"https://github.com/SealabJaster/PersonalWebsite/blob/master/PersonalWebsite/wwwroot/{relativePath}",
                                                          OrderInSeries     = order++
                                                      };
                                                  }).ToList()
                                     };
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

        private bool FindFirstMatchingMetadata(MarkdownDocument document, out string value, params string[] keys)
        {
            foreach(var key in keys)
            {
                if(this.FindMetadata(document, key, out value))
                    return true;
            }

            value = null;
            return false;
        }

        private DateTimeOffset FindRequiredMetadataAsDate(MarkdownDocument document, string key)
        {
            if (!this.FindMetadata(document, key, out string dateString))
                throw new InvalidDataException($"The blog post is missing the required metadata '@{key}'");

            return DateTimeOffset.ParseExact(dateString.Trim(), "dd-MM-yyyy", null);
        }

        private string FindRequiredMetadataAsText(MarkdownDocument document, string key)
        {
            if (!this.FindMetadata(document, key, out string text))
                throw new InvalidDataException($"The blog post is missing the required metadata '@{key}'");

            return text;
        }

        private string FindMetadataAsText(MarkdownDocument document, string key)
        {
            var found = this.FindMetadata(document, key, out string text);
            return (found) ? text : null;
        }

        private string FindFirstMatchingMetadataAsText(MarkdownDocument document, params string[] keys)
        {
            if(!this.FindFirstMatchingMetadata(document, out string text, keys))
                throw new InvalidDataException($"The blog post is missing all of the following potential keys: {keys}");

            return text;
        }
    }
}
