using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NuGetTools.Common
{
    public static class HttpUtil
    {
        public static string GetHttpResponse(
            string uri,
            Logger log)
        {
            var responseString = string.Empty;
            var httpRequest = (HttpWebRequest)WebRequest.Create(uri);
            httpRequest.Method = WebRequestMethods.Http.Get;
            httpRequest.UserAgent = "nugettools";

            log.Info($"Making HTTP request: {uri}");

            using (var httpResponse = httpRequest.GetResponse())
            using (var dataStream = httpResponse.GetResponseStream())
            {
                var reader = new StreamReader(dataStream);
                var objResponse = reader.ReadToEnd();
                responseString = objResponse as string;
            }

            return responseString;
        }
    }
}
