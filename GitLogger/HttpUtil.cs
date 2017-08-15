using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace GitLogger
{
    public static class HttpUtil
    {
        private static readonly string commitListRequestUri = @"https://api.github.com/repos/";
        private static readonly string commitMetadataRequestUri = $@"https://api.github.com/search/issues";
        private static readonly string RateLimitRequestUri = $@"https://api.github.com/rate_limit";

        public static IList<Commit> GetCommits(string repository, string startSha, string cachePath)
        {
            var commits = new List<Commit>();
            var uri = commitListRequestUri + $"{repository}/commits?per_page=100";

            var responseString = GetCachedOrHttpResponse(uri, cachePath);

            var jArray = JArray.Parse(responseString);

            foreach (var entry in jArray)
            {
                var commit = new Commit(entry);
                if (commit.Sha == startSha)
                {
                    break;
                }
                commits.Add(commit);
            }

            return commits;
        }

        public static void UpdateWithMetadata(Commit commit, string cachePath)
        {

            var uri = commitMetadataRequestUri + $"?q={commit.Sha}+type:pr";

            var responseString = GetCachedOrHttpResponse(uri, cachePath);

            var jObject = JObject.Parse(responseString);
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
                var prUrl = item.Value<string>("html_url");
                var prId = GetIdFromUrl(prUrl);

                if (prId > 0)
                {
                    commit.PR = Tuple.Create(prId, prUrl);
                }

                var body = item.Value<string>("body");
                var issueUrls = GetIssuesFromPrBody(body);

                if (issueUrls.Count() > 1)
                {
                    Console.WriteLine($"WARNING: Multiple issues found in PR body.");
                }

                var issues = new List<Tuple<int, string>>(); 
                foreach(var issueUrl in issueUrls)
                {
                    var issueId = GetIdFromUrl(issueUrl);

                    if (issueId > 0)
                    {
                        issues.Add(Tuple.Create(issueId, issueUrl));
                    }
                }
                commit.Issues = issues;
            }
        }


        public static void BlockTillRateLimitRefresh()
        {
            var httpRequest = (HttpWebRequest)WebRequest.Create(RateLimitRequestUri);
            httpRequest.Method = WebRequestMethods.Http.Get;
            httpRequest.Accept = "application/json";
            httpRequest.UserAgent = "gitlogger";

            using (var httpResponse = httpRequest.GetResponse())
            using (var dataStream = httpResponse.GetResponseStream())
            {                

                var reader = new StreamReader(dataStream);
                object objResponse = reader.ReadToEnd();

                var responseJObject = JObject.Parse(objResponse as string);
                var rateJObject = responseJObject.Value<JObject>("rate");
                var limit = rateJObject.Value<int>("limit");
                var remaining = rateJObject.Value<int>("remaining");
                var reset = rateJObject.Value<double>("reset");

                if (remaining == 0)
                {
                    var timeOut = TimeSpan.FromSeconds(reset);
                    var dt = new DateTime(timeOut.Ticks);
                    Console.WriteLine($"WARNING: Github api rate limit reached. Sleeping till {dt.ToLocalTime().ToShortTimeString()}");

                    Thread.Sleep(timeOut);
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

        public static IList<string> GetIssuesFromPrBody(string body)
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
                    result.Add(word);
                }
            }
            return result;
        }

        public static string GetCachedOrHttpResponse(string uri, string cachePath)
        {
            var responseString = string.Empty;

            var httpRequest = (HttpWebRequest)WebRequest.Create(uri);
            httpRequest.Method = WebRequestMethods.Http.Get;
            httpRequest.Accept = "application/json";
            httpRequest.UserAgent = "gitlogger";

            var cacheFilePath = Path.Combine(cachePath, httpRequest.Address.AbsolutePath.Replace('/', '_') + ".json");

            if (File.Exists(cacheFilePath))
            {                
                responseString = FileUtil.GetCachedResponse(cacheFilePath);
            }
            else
            {
                Console.WriteLine($"Making HTTP request: {uri}");

                BlockTillRateLimitRefresh();

                using (var httpResponse = httpRequest.GetResponse())
                using (var dataStream = httpResponse.GetResponseStream())
                {
                    Console.WriteLine($"Github api rate limit left: {httpResponse.Headers.Get("X-RateLimit-Remaining")}");
                    var reader = new StreamReader(dataStream);
                    object objResponse = reader.ReadToEnd();

                    FileUtil.CacheResponse(cacheFilePath, objResponse as string);

                    responseString = objResponse as string;
                }
            }

            return responseString;
        }
    }
}
