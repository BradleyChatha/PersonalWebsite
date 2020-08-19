using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PersonalWebsite.Constants
{
    public static class WebConstants
    {
        public const           string WEBSITE_URL      = "https://bradley.chatha.dev";
        public static readonly string DEFAULT_OG_IMAGE = WebConstants.FilePathToUrl("img/default_og_image.jpg");

        public static string FilePathToUrl(string filePath)
        {
            if(filePath == null)
                return null;

            if(filePath.Length == 0)
                return WEBSITE_URL;
               
            if(filePath.StartsWith("http"))
                return filePath;

            return (filePath[0] == '/')
                   ? WEBSITE_URL + filePath
                   : WEBSITE_URL + '/' + filePath;
        }
    }
}
