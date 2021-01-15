using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PersonalWebsite.Services
{
    #region Data types
    public class SitemapGeneratorOptions
    {
        public IList<ISitemapContentProvider> ContentProviders { get; set; }

        public SitemapGeneratorOptions()
        {
            this.ContentProviders = new List<ISitemapContentProvider>();
        }
    }

    public enum SitemapFrequency
    {
        Weekly,
        Monthly,
        Yearly
    }

    public class SitemapContent
    {
        public string Loc { get; set; }
        public SitemapFrequency ChangeFreq { get; set; }
        public float Priority { get; set; }
    }
    #endregion

    #region Interfaces
    public interface ISitemapGenerator
    {
        XDocument GenerateSitemap();
    }

    public interface ISitemapContentProvider
    {
        IEnumerable<SitemapContent> GetContent();
    }

    public interface ISitemapServicedContentProvider : ISitemapContentProvider
    {
        void ConstructProvider(IServiceProvider services);
    }
    #endregion

    #region Implementations
    public class SitemapGenerator : ISitemapGenerator
    {
        readonly SitemapGeneratorOptions _options;
        readonly IServiceProvider        _services;
                 XDocument               _cachedSitemap;

        public SitemapGenerator(SitemapGeneratorOptions options, IServiceProvider services)
        {
            this._options  = options;
            this._services = services;
        }

        public XDocument GenerateSitemap()
        {
            if(this._cachedSitemap != null)
                return this._cachedSitemap;

            XNamespace xmlns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            var document = new XDocument();
            document.Add(new XElement(
                xmlns + "urlset",
                this._options
                    .ContentProviders
                    .SelectMany(p => 
                    {
                        if (p is ISitemapServicedContentProvider servicedProvider)
                            servicedProvider.ConstructProvider(this._services);

                        return p.GetContent();
                    })
                    .Select(c => new XElement(xmlns + "url",
                        new XElement(xmlns + "loc",        c.Loc),
                        new XElement(xmlns + "changefreq", Enum.GetName(typeof(SitemapFrequency), c.ChangeFreq).ToLower()),
                        new XElement(xmlns + "priority",   Math.Round(c.Priority, 1))
                    ))
            ));

            this._cachedSitemap = document;
            return document;
        }
    }

    public class StaticSitemapProvider : ISitemapContentProvider
    {
        readonly SitemapContent _content;

        public StaticSitemapProvider(string url, SitemapFrequency changefreq, float priority)
        {
            this._content = new SitemapContent 
            {
                Loc         = url,
                ChangeFreq  = changefreq,
                Priority    = priority
            };
        }

        public IEnumerable<SitemapContent> GetContent()
        {
            return new[] { this._content };
        }
    }

    public class ServicedSitemapProvider<T> : ISitemapServicedContentProvider
    where T : ISitemapContentProvider
    {
        T _provider;

        public void ConstructProvider(IServiceProvider services)
        {
            this._provider = services.GetRequiredService<T>();
        }

        public IEnumerable<SitemapContent> GetContent()
        {
            return this._provider.GetContent();
        }
    }

    // This is a service, so wrap inside ServicedSitemapProvider.
    public class BlogSitemapProvider : ISitemapContentProvider
    {
        readonly IBlogProvider _blogs;

        public BlogSitemapProvider(IBlogProvider blogs)
        {
            this._blogs = blogs;
        }

        public IEnumerable<SitemapContent> GetContent()
        {
            return this._blogs
                       .GetBlogSeries()
                       .Select(s => 
                       { 
                           var entries = s.Posts
                                          .Select(b => new SitemapContent
                                          {
                                              Loc        = $"https://bradley.chatha.dev/{s.GetPostSeoPath(b)}",
                                              ChangeFreq = SitemapFrequency.Monthly,
                                              Priority   = 0.7f
                                          }).ToList();
                            
                           if(!s.Series.IsSingleSeries)
                           {
                               entries.Add(new SitemapContent
                               {
                                   Loc = $"https://bradley.chatha.dev/Blog/{s.Series.Reference}",
                                   ChangeFreq = SitemapFrequency.Weekly,
                                   Priority = 0.8f
                               });
                           }

                           return entries;
                        }
                       )
                       .SelectMany(c => c);
        }
    }
    #endregion

    public static class Extensions
    { 
        public static void AddSitemapGenerator(this IServiceCollection services, Action<SitemapGeneratorOptions> configurator)
        {
            var options = new SitemapGeneratorOptions();
            configurator(options);

            services.AddSingleton<ISitemapGenerator>(new SitemapGenerator(options, services.BuildServiceProvider()));
        }
    }
}
