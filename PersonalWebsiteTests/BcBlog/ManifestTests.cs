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
    [reference      ""test""]
	[name			""abc""]
    [description    ""a""
                    ""b""
                    ""c""
    ]
    [post           ""test/blog.bcb""]
    [tags           ""super""
                    ""blog""
    ]
}"
            );

            var manifest = new Manifest(parser);
            Assert.AreEqual(1, manifest.Series.Count);
            
            var series = manifest.Series.First();
            Assert.AreEqual("abc",   series.Name);
            Assert.AreEqual("a b c", series.Description);
            Assert.AreEqual("test",  series.Reference);
            Assert.IsTrue(series.PostFilePaths.SequenceEqual(new[]
            {
                "test/blog.bcb"
            }));
            Assert.IsTrue(series.Tags.SequenceEqual(new[]
            {
                "super",
                "blog"
            }));
        }
    }
}