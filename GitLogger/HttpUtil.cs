using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace GitLogger
{
    public static class HttpUtil
    {
        private static readonly string commitListRequestUri = @"https://api.github.com/repos/nuget/nuget.client/commits";
        private static readonly string commitMetadataRequestUri = $@"https://api.github.com/search/issues";

        public static IList<Commit> GetCommits(string startSha)
        {
            var httpRequest = (HttpWebRequest)WebRequest.Create(commitListRequestUri + "?per_page=100");
            httpRequest.Method = WebRequestMethods.Http.Get;
            httpRequest.Accept = "application/json";
            httpRequest.UserAgent = "gitlogger";

            var commits = new List<Commit>();
            using (var theResponse = httpRequest.GetResponse())
            {
                var dataStream = theResponse.GetResponseStream();
                var reader = new StreamReader(dataStream);
                object objResponse = reader.ReadToEnd();
                dataStream.Close();
                theResponse.Close();

                var jArray = JArray.Parse(objResponse as string);

                foreach (var entry in jArray)
                {
                    var commit = new Commit(entry);
                    commits.Add(commit);
                }
            }

            return commits;
        }

        public static void UpdateWithMetadata(Commit commit)
        {
            var httpRequest = (HttpWebRequest)WebRequest.Create(commitMetadataRequestUri + $"?q={commit.Sha}+type:pr");
            httpRequest.Method = WebRequestMethods.Http.Get;
            httpRequest.Accept = "application/json";
            httpRequest.UserAgent = "gitlogger";

            var commits = new List<Commit>();
            using (var theResponse = httpRequest.GetResponse())
            {
                var dataStream = theResponse.GetResponseStream();
                var reader = new StreamReader(dataStream);
                object objResponse = reader.ReadToEnd();
                dataStream.Close();
                theResponse.Close();

                var jObject = JObject.Parse(objResponse as string);
                var incompleteResults = jObject.Value<bool>("incomplete_results");
                var totalCount = jObject.Value<int>("total_count");

                if (incompleteResults)
                {
                    Console.WriteLine($"WARNING: Incomplete results while querying for {commit.Sha}" + 
                        Environment.NewLine + 
                        $"Try: {commitMetadataRequestUri}?q={commit.Sha}+type:pr");
                }
                if (totalCount != 1)
                {
                    Console.WriteLine($"ERROR: Multiple results while querying for {commit.Sha}" +
                        Environment.NewLine +
                        $"Try: {commitMetadataRequestUri}?q={commit.Sha}+type:pr");
                }
                else
                {
                    var item = jObject.Value<JArray>("items")[0];
                    var url = item.Value<string>("html_url");
                    var id = GetIdFromUrl(url);

                    if (id > 0)
                    {
                        commit.PR = Tuple.Create(id, url);
                    }
                }

            }
        }

        public static int GetIdFromUrl(string url)
        {
            var result = -1;

            var contents = url.Split('/');

            if (contents.Any())
            {
                var idStr = contents.Last();
                result = Int32.Parse(idStr);
            }

            return result;
        }
    }
}
