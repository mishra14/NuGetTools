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
        private static readonly string commitListRequestUri = @"https://api.github.com/repos/";
        private static readonly string commitMetadataRequestUri = $@"https://api.github.com/search/issues";

        public static IList<Commit> GetCommits(string repository, string startSha)
        {
            var httpRequest = (HttpWebRequest)WebRequest.Create(commitListRequestUri+ $"{repository}/commits?per_page=100");
            httpRequest.Method = WebRequestMethods.Http.Get;
            httpRequest.Accept = "application/json";
            httpRequest.UserAgent = "gitlogger";

            var commits = new List<Commit>();
            using (var httpResponse = httpRequest.GetResponse())
            using (var dataStream = httpResponse.GetResponseStream())
            {

                Console.WriteLine($"Github api rate limit left: {httpResponse.Headers.Get("X-RateLimit-Remaining")}");

                var reader = new StreamReader(dataStream);
                object objResponse = reader.ReadToEnd();

                var jArray = JArray.Parse(objResponse as string);

                foreach (var entry in jArray)
                {
                    var commit = new Commit(entry);
                    if (commit.Sha == startSha)
                    {
                        break;
                    }
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
            using (var httpResponse = httpRequest.GetResponse())
            using (var dataStream = httpResponse.GetResponseStream())
            {
                var reader = new StreamReader(dataStream);
                object objResponse = reader.ReadToEnd();

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

                    var body = item.Value<string>("body");
                    var issueUrls = GetIssuesFromPRBody(body);

                    if (issueUrls.Count() > 1)
                    {

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

        public static IList<string> GetIssuesFromPRBody(string body)
        {
            var result = new List<string>();

            var splitChars = new char[] { '.', '?', '!', ' ', ';', ':', ',', '\r', '\n' };
            var words = body.Split(splitChars, StringSplitOptions.RemoveEmptyEntries).Select(w => w.ToLowerInvariant());

            foreach (var word in words)
            {
                if (word.Contains("nuget") &&
                    word.Contains("home") &&
                    word.Contains("issues"))
                {
                    result.Append(word);
                }
            }
            return result;
        }
    }
}
