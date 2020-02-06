using Microsoft.VisualStudio.TestTools.UnitTesting;
using PersonalWebsite.BcBlog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersonalWebsite.BcBlog.Tests
{
    [TestClass()]
    public class ManifestTests
    {
        [TestMethod()]
        public void ManifestTest()
        {
            var parser = new ManifestParser(
@"@series
{
	[name			""abc""]
    [description    ""a""
                    ""b""
                    ""c""
    ]
    [date-released   $31-10-2019-+0]
    [date-updated	 $25-12-2019-+0]
    [post            ""test/blog.bcb""]
}"
            );

            var manifest = new Manifest(parser);
            Assert.AreEqual(1, manifest.Series.Count);
            
            var series = manifest.Series.First();
            Assert.AreEqual("abc", series.Name);
            Assert.AreEqual("a b c", series.Description);
            Assert.AreEqual(new DateTimeOffset(2019, 10, 31, 0, 0, 0, TimeSpan.FromSeconds(0)), series.DateReleased);
            Assert.AreEqual(new DateTimeOffset(2019, 12, 25, 0, 0, 0, TimeSpan.FromSeconds(0)), series.DateUpdated);
            Assert.IsTrue(series.PostFilePaths.SequenceEqual(new[]
            {
                "test/blog.bcb"
            }));
        }
    }
}