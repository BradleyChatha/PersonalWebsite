using Microsoft.AspNetCore.Http;
using PersonalWebsite.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace PersonalWebsite.Extensions
{
    public static class HttpClientExtensions
    {
        public static Task PostMatomoEventAsync(
            this HttpClient client,
            string category,
            string action,
            string name = null
        )
        {
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                new Uri($"mphp" +
                        $"?idsite={MatomoConstants.SITE_ID}" +
                        $"&rec=1" +
                        $"&uid=SERVER" +
                        $"&_id=0123456789ABCDEF" +
                        $"&cid=0123456789ABCDEF" +
                        $"&e_c={category}" +
                        $"&e_a={action}" +
                        $"{(name != null ? "&e_n="+name : null)}",
                        UriKind.Relative
                )
            );

            return client.SendAsync(request);
        }
    }
}
