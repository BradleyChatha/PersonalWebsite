using Microsoft.VisualStudio.TestTools.UnitTesting;
using PersonalWebsite.BcBlog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Tok  = PersonalWebsite.BcBlog.BcToken<PersonalWebsite.BcBlog.ManifestParser.TokenType>;
using TokT = PersonalWebsite.BcBlog.ManifestParser.TokenType;

namespace PersonalWebsite.BcBlog.Tests
{
    [TestClass()]
    public class ManifestParserTests
    {
        [TestMethod()]
        public void TestParser()
        {
            var parser = new ManifestParser(
@"@series
{
	[name			""abc""]
    [description    ""a""
                    ""b""
                    ""c""
    ]
}"
            );

            Assert.IsTrue(parser.SequenceEqual(new[] { 
                new Tok{ Type = TokT.SeriesMarker                               },
                new Tok{ Type = TokT.LBracket                                   },

                new Tok{ Type = TokT.LSquareBracket                             },
                new Tok{ Type = TokT.Identifier,    Value = "name"              },
                new Tok{ Type = TokT.String,        Value = "abc"               },
                new Tok{ Type = TokT.RSquareBracket                             },

                new Tok{ Type = TokT.LSquareBracket                             },
                new Tok{ Type = TokT.Identifier,    Value = "description"       },
                new Tok{ Type = TokT.String,        Value = "a"                 },
                new Tok{ Type = TokT.String,        Value = "b"                 },
                new Tok{ Type = TokT.String,        Value = "c"                 },
                new Tok{ Type = TokT.RSquareBracket                             },

                new Tok{ Type = TokT.RBracket                                   },
                new Tok{ Type = TokT.EoF                                        },
            }));
        }
    }
}