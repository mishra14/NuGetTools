using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace GitLogger
{
    public static class HttpUtil
    {
        private static readonly string githubApiUri = @"https://api.github.com/repos/";
        private static readonly string githubSearchUri = @"https://api.github.com/search/issues";
        private static readonly string RateLimitRequestUri = @"https://api.github.com/rate_limit";
        private static readonly string clientParams = @"client_id={0}&client_secret={1}";

        public static IList<Commit> GetCommits(
            string repository,
            string startSha,
            string cachePath,
            Tuple<string, string> clientDetails)
        {
            var commits = new List<Commit>();

            var page = 1;
            var done = false;

            while (!done)
            {
                var uri = githubApiUri + $"{repository}/commits?per_page=100&page={page++}";

                var responseString = GetCachedOrHttpResponse(uri, cachePath, clientDetails, isRequestTypeSearch: false, useCache: false);

                var jArray = JArray.Parse(responseString);

                if (jArray.Count == 0)
                {
                    Console.WriteLine("WARNING: Exiting commit lookup without finding the original commit");
                    break;
                }

                foreach (var entry in jArray)
                {
                    var commit = new Commit(entry);
                    if (commit.Sha == startSha)
                    {
                        done = true;
                        break;
                    }
                    commits.Add(commit);
                }
            }
            return commits;
        }

        public static IList<Commit> GetPullRequests(
            string repository,
            string startSha,
            string cachePath,
            Tuple<string, string> clientDetails)
        {
            var commits = new List<Commit>();

            return commits;
        }

        public static void UpdateWithMetadata(
            string repository,
            IList<Commit> commits,
            string cachePath,
            Tuple<string, string> clientDetails)
        {
            var commitLookUp = new Dictionary<string, Commit>();

            foreach (var commit in commits)
            {
                commitLookUp.Add(commit.Sha, commit);
            }

            var page = 1;

            while (commitLookUp.Count > 0)
            {
                var uri = githubApiUri + $"{repository}/pulls?state=all&per_page=100&page={page++}";

                var responseString = GetCachedOrHttpResponse(uri, cachePath, clientDetails, isRequestTypeSearch: false, useCache: false);

                var jArray = JArray.Parse(responseString);

                if (jArray.Count == 0)
                {
                    Console.WriteLine("WARNING: Exiting PR lookup without finiding PRs for all commits");
                    break;
                }

                foreach (var entry in jArray)
                {
                    // Get merge commit sha
                    var commitSha = entry.Value<string>("merge_commit_sha");

                    // If the sha is in the look up, then get more info
                    if (!string.IsNullOrEmpty(commitSha) && commitLookUp.TryGetValue(commitSha, out var commit))
                    {
                        // Get PR data
                        var prUrl = entry.Value<string>("html_url");
                        var prId = GetIdFromUrl(prUrl);

                        if (prId > 0)
                        {
                            commit.PR = Tuple.Create(prId, prUrl);
                        }

                        // Get body and issue data
                        var body = entry.Value<string>("body");
                        var issueUrls = GetLinks(body);

                        if (issueUrls.Count() > 1)
                        {
                            Console.WriteLine($"WARNING: Multiple issues found in PR body.");
                        }

                        var issues = new HashSet<Tuple<int, string>>();
                        foreach (var issueUrl in issueUrls)
                        {
                            var issueId = GetIdFromUrl(issueUrl);

                            if (issueId > 0)
                            {
                                issues.Add(Tuple.Create(issueId, issueUrl));
                            }
                        }

                        commit.Issues = issues;

                        commitLookUp.Remove(commit.Sha);

                        if (commitLookUp.Count == 0)
                        {
                            break;
                        }
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

        // Source - https://stackoverflow.com/questions/9125016/get-url-from-a-text
        public static List<string> GetLinks(string message)
        {
            List<string> list = new List<string>();
            Regex urlRx = new Regex(@"((https?|ftp|file)\://|www.)[A-Za-z0-9\.\-]+(/[A-Za-z0-9\?\&\=;\+!'\(\)\*\-\._~%]*)*", 
                RegexOptions.IgnoreCase);

            MatchCollection matches = urlRx.Matches(message);
            foreach (Match match in matches)
            {
                var url = match.Value.ToLowerInvariant();
                if (url.Contains("nuget") &&
                    url.Contains("home") &&
                    url.Contains("issues"))
                {
                    if (Regex.IsMatch(url, @"[\D]*$"))
                    {
                        var regex = new Regex(@"[\D]*$");
                        url = regex.Replace(url, "");
                    }

                    list.Add(url);
                }
            }

            return list;
        }

        public static string GetCachedOrHttpResponse(
            string uri,
            string cachePath,
            Tuple<string, string> clientDetails,
            bool isRequestTypeSearch,
            bool useCache)
        {
            var responseString = string.Empty;
            var uriWithClientData = uri + "&" + string.Format(clientParams, clientDetails.Item1, clientDetails.Item2);
            var httpRequest = (HttpWebRequest)WebRequest.Create(uriWithClientData);
            httpRequest.Method = WebRequestMethods.Http.Get;
            httpRequest.Accept = "application/json";
            httpRequest.UserAgent = "gitlogger";

            var cacheFileName = httpRequest.Address.AbsolutePath + httpRequest.Address.Query + ".json";
            cacheFileName = cacheFileName.Replace('/', '_')
                .Replace('?', '_')
                .Replace('=', '_')
                .Replace('+', '_')
                .Replace(':', '_');

            var cacheFilePath = Path.Combine(cachePath, cacheFileName);

            if (useCache && File.Exists(cacheFilePath))
            {
                responseString = FileUtil.GetCachedResponse(cacheFilePath);
            }
            else
            {
                Console.WriteLine($"Making HTTP request: {uriWithClientData}");

                BlockTillRateLimitRefresh(clientDetails, isRequestTypeSearch);

                using (var httpResponse = httpRequest.GetResponse())
                using (var dataStream = httpResponse.GetResponseStream())
                {
                    Console.WriteLine($"Github api rate limit left: {httpResponse.Headers.Get("X-RateLimit-Remaining")}");
                    var reader = new StreamReader(dataStream);
                    var objResponse = reader.ReadToEnd();

                    if (useCache)
                    {
                        FileUtil.CacheResponse(cacheFilePath, objResponse as string);
                    }
               
                    responseString = objResponse as string;
                }
            }

            return responseString;
        }

        private static void BlockTillRateLimitRefresh(Tuple<string, string> clientDetails, bool isRequestTypeSearch)
        {
            var uriWithClientData = RateLimitRequestUri + "?" + string.Format(clientParams, clientDetails.Item1, clientDetails.Item2);
            var httpRequest = (HttpWebRequest)WebRequest.Create(uriWithClientData);
            httpRequest.Method = WebRequestMethods.Http.Get;
            httpRequest.Accept = "application/json";
            httpRequest.UserAgent = "gitlogger";

            using (var httpResponse = httpRequest.GetResponse())
            using (var dataStream = httpResponse.GetResponseStream())
            {

                var reader = new StreamReader(dataStream);
                object objResponse = reader.ReadToEnd();

                var responseJObject = JObject.Parse(objResponse as string);
                var resourcesJObject = responseJObject.Value<JObject>("resources");
                var rateJObject = resourcesJObject.Value<JObject>("core");

                if (isRequestTypeSearch)
                {
                    rateJObject = resourcesJObject.Value<JObject>("search");
                }

                var limit = rateJObject.Value<int>("limit");
                var remaining = rateJObject.Value<int>("remaining");
                var reset = rateJObject.Value<double>("reset");

                if (remaining == 0)
                {
                    var timeOut = TimeSpan.FromSeconds(reset);
                    var dt = new DateTime(timeOut.Ticks);
                    Console.WriteLine($"WARNING: Github api rate limit reached. Sleeping till {dt.ToLocalTime()}");

                    Thread.Sleep(timeOut.Subtract(TimeSpan.FromTicks(DateTime.Now.ToUniversalTime().Ticks)));
                }
            }

        }
    }
}
