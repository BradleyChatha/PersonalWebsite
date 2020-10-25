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
        // "SEO V1" is where the post's URL has the series' tags appended to it.
        // I'm pretty sure though that this is triggering the keyword stuffing in Google, so from now on we'll just not do that.
        public bool UsesSeoV1 => this.Posts.Any(p => p?.DateCreated < new DateTimeOffset(2019, 10, 16, 0, 0, 0, TimeSpan.Zero));
        public bool UsesSeoV2 => !UsesSeoV1;

        public Uri GetPostSeoPath(int postIndex)
        {
            var post   = this.Posts[postIndex];
            var values = new List<string>()
            {
                Convert.ToString(post.OrderInSeries),
                post.SeoTag
            };

            if(this.UsesSeoV1)
                values.Add(this.Series.Tags.Aggregate((a,b) => a+'-'+b));

            // SEO V2 allows posts to specify their own canonical URL.
            if(this.UsesSeoV2 && post.SeoUrl != null)
                return new Uri(post.SeoUrl.TrimStart('/'), UriKind.Relative);
            else
                return new Uri($"BlogPost/{this.Series.Reference}/{values.Aggregate((a,b) => a+'-'+b)}", UriKind.Relative);
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
        public string           SeoUrl              { get; set; }
    }

    public interface IBlogProvider
    {
        IEnumerable<BlogSeriesAndPosts> GetBlogSeries();
        BlogSeriesAndPosts GetBlogPostFromSeoUrlOrNull(string url, out int index);
    }

    public class CachingBlogProvider : IBlogProvider
    {
        struct BlogPostInfo
        {
            public BlogSeriesAndPosts SeriesAndPosts;
            public int Index;
        }

        readonly IWebHostEnvironment      _environment;
        IEnumerable<BlogSeriesAndPosts>   _seriesCache;
        IDictionary<string, BlogPostInfo> _infoBySeoUrl { get; set; }

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
                                                          SeoUrl            = this.FindMetadataAsText(document, "seo-url"),
                                                          CardImageGeneric  = this.FindMetadataAsText(document, "card-image"),
                                                          CardImageTwitter  = this.FindMetadataAsText(document, "card-image-twitter"),
                                                          GithubUrl         = $"https://github.com/BradleyChatha/PersonalWebsite/blob/master/PersonalWebsite/wwwroot/{relativePath}",
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

        public BlogSeriesAndPosts GetBlogPostFromSeoUrlOrNull(string url, out int index)
        {
            bool exists = this.GetSeoUrlMetadata().TryGetValue(url, out BlogPostInfo info);

            index = (exists) ? info.Index : -1;
            return info.SeriesAndPosts;
        }

        IDictionary<string, BlogPostInfo> GetSeoUrlMetadata()
        {
            if(this._infoBySeoUrl == null)
            {
                this._infoBySeoUrl = new Dictionary<string, BlogPostInfo>();

                foreach(var series in this.GetBlogSeries())
                {
                    for (int i = 0; i < series.Posts.Count; i++)
                    {
                        var post = series.Posts[i];
                        if (post.SeoUrl == null)
                            continue;

                        if(this._infoBySeoUrl.ContainsKey(post.SeoUrl))
                            throw new InvalidOperationException($"Duplicate SeoUrl: {post.SeoUrl}");

                        this._infoBySeoUrl[post.SeoUrl] = new BlogPostInfo
                        {
                            Index = i,
                            SeriesAndPosts = series
                        };
                    }
                }
            }

            return this._infoBySeoUrl;
        }
    }
}
