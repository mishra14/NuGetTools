using Newtonsoft.Json.Linq;
using NuGetTools.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace GitLogger.Library
{
    public class HttpUtil
    {
        //private  readonly string githubSearchUri = @"https://api.github.com/search/issues";

        private readonly string githubApiUri = @"https://api.github.com/repos/";
        private readonly string RateLimitRequestUri = @"https://api.github.com/rate_limit";
        private readonly string clientParams = @"client_id={0}&client_secret={1}";

        private static DateTime _epoch = new DateTime(1970, 1, 1);

        private Logger _log;

        public HttpUtil(Logger log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public IList<Commit> GetCommits(
            string repository,
            string branch,
            string startSha,
            Tuple<string, string> clientDetails)
        {
            var commits = new List<Commit>();
            var page = 1;
            var done = false;

            while (!done)
            {
                var uri = githubApiUri + $"{repository}/commits?sha={branch}&per_page=100&page={page++}";
                var responseString = GetHttpResponse(uri, clientDetails, isRequestTypeSearch: false);
                var jArray = JArray.Parse(responseString);

                if (jArray.Count == 0)
                {
                    _log.Warning("WARNING: Exiting commit lookup without finding the original commit");
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

        public void UpdateWithMetadata(
            string codeRepository,
            string issueRepository,
            IList<Commit> commits,
            Tuple<string, string> clientDetails)
        {
            var commitLookUp = new Dictionary<string, Commit>();
            var prToCommitLookUp = new Dictionary<int, Commit>();

            foreach (var commit in commits)
            {
                var pr = GetPrFromCommit(commit, codeRepository);

                if (pr != null)
                {
                    if (prToCommitLookUp.ContainsKey(pr.Item1))
                    {
                        _log.Warning($"WARNING: PR {pr.Item1} is related to commit {commit} and commit {prToCommitLookUp[pr.Item1]}");
                    }
                    else
                    {
                        prToCommitLookUp.Add(pr.Item1, commit);
                    }
                }

                commitLookUp.Add(commit.Sha, commit);
            }

            var page = 1;
            while (commitLookUp.Count > 0)
            {
                var uri = githubApiUri + $"{codeRepository}/pulls?state=all&per_page=100&page={page++}";
                var responseString = GetHttpResponse(uri, clientDetails, isRequestTypeSearch: false);
                var jArray = JArray.Parse(responseString);

                if (jArray.Count == 0)
                {
                    _log.Warning("WARNING: Exiting PR lookup without finiding PRs for all commits");
                    break;
                }

                foreach (var entry in jArray)
                {
                    // Get prId
                    var prId = Int32.Parse(entry.Value<string>("number"));

                    // Get merge commit sha
                    var commitSha = entry.Value<string>("merge_commit_sha");

                    // If the sha is in the look up, then get more info
                    if (!string.IsNullOrEmpty(commitSha) && commitLookUp.TryGetValue(commitSha, out var commit))
                    {
                        UpdateWithMetadataFromJToken(entry, commit, issueRepository);
                        commitLookUp.Remove(commit.Sha);

                        if (commitLookUp.Count == 0)
                        {
                            break;
                        }
                    }
                    else if (prToCommitLookUp.TryGetValue(prId, out commit))
                    {
                        UpdateWithMetadataFromJToken(entry, commit, issueRepository);
                        commitLookUp.Remove(commit.Sha);

                        if (commitLookUp.Count == 0)
                        {
                            break;
                        }
                    }
                }
            }

            // incase of release branch commits, we dont have PRs, so try to parse the commit text to figure out the issue
            foreach (var commit in commits)
            {
                if (commit.Issues == null || commit.Issues.Count == 0)
                {
                    UpdateCommitIssuesFromText(commit.Message, commit, issueRepository);
                }
            }
        }

        private void UpdateWithMetadataFromJToken(
            JToken entry,
            Commit commit,
            string issueRepository)
        {
            // Get PR data
            var prUrl = entry.Value<string>("html_url");
            var prId = GetIdFromUrl(prUrl);

            if (prId > 0)
            {
                commit.PR = Tuple.Create(prId, prUrl);
            }

            var body = entry.Value<string>("body");
            UpdateCommitIssuesFromText(body, commit, issueRepository);
        }

        private Tuple<int, string> GetPrFromCommit(Commit commit, string codeRepository)
        {
            var message = commit.Message;
            var urlRx = new Regex(@"(#\d+)", RegexOptions.IgnoreCase);

            var matches = urlRx.Matches(message);
            foreach (Match match in matches)
            {
                var prId = match.Value.Substring(1, match.Length - 1);
                var prUrl = $"{codeRepository}/pull/{prId}";

                return Tuple.Create(GetIdFromUrl(prUrl), prUrl);
            }

            return null;
        }

        private void UpdateCommitIssuesFromText(
            string body,
            Commit commit,
            string issueRepository)
        {
            // Get issue data
            var issueUrls = GetLinks(body, issueRepository);

            if (issueUrls.Count() > 1)
            {
                _log.Warning($"WARNING: Multiple issues found in PR body for commit '{commit.Sha}'.");
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
        }

        public int GetIdFromUrl(string url)
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
        public List<string> GetLinks(
            string message,
            string issueRepository)
        {
            List<string> list = new List<string>();
            Regex urlRx = new Regex(@"((https?|ftp|file)\://|www.)[A-Za-z0-9\.\-]+(/[A-Za-z0-9\?\&\=;\+!'\(\)\*\-\._~%]*)*",
                RegexOptions.IgnoreCase);

            MatchCollection matches = urlRx.Matches(message);
            foreach (Match match in matches)
            {
                var url = match.Value.ToLowerInvariant();

                foreach (var issueRepo in issueRepository.Split(';'))
                {
                    var repo = issueRepo.ToLowerInvariant();
                    if (url.Contains(repo) &&
                        (url.Contains("issues") || url.Contains("_workitems")))
                    {
                        if (Regex.IsMatch(url, @"[\D]*$"))
                        {
                            var regex = new Regex(@"[\D]*$");
                            url = regex.Replace(url, "");
                        }

                        list.Add(url);
                    }
                }
            }

            return list;
        }


        // Not used since an API key was added which increases the limit on api calls
        public string GetCachedOrHttpResponse(
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
                _log.Info($"Making HTTP request: {uriWithClientData}");

                BlockTillRateLimitRefresh(clientDetails, isRequestTypeSearch);

                using (var httpResponse = httpRequest.GetResponse())
                using (var dataStream = httpResponse.GetResponseStream())
                {
                    _log.Info($"Github api rate limit left: {httpResponse.Headers.Get("X-RateLimit-Remaining")}");
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

        public string GetHttpResponse(
            string uri,
            Tuple<string, string> clientDetails,
            bool isRequestTypeSearch)
        {
            var responseString = string.Empty;
            var uriWithClientData = uri + "&" + string.Format(clientParams, clientDetails.Item1, clientDetails.Item2);
            var httpRequest = (HttpWebRequest)WebRequest.Create(uriWithClientData);
            httpRequest.Method = WebRequestMethods.Http.Get;
            httpRequest.Accept = "application/json";
            httpRequest.UserAgent = "gitlogger";

            _log.Info($"Making HTTP request: {uriWithClientData}");

            BlockTillRateLimitRefresh(clientDetails, isRequestTypeSearch);

            using (var httpResponse = httpRequest.GetResponse())
            using (var dataStream = httpResponse.GetResponseStream())
            {
                _log.Info($"Github api rate limit left: {httpResponse.Headers.Get("X-RateLimit-Remaining")}");
                var reader = new StreamReader(dataStream);
                var objResponse = reader.ReadToEnd();
                responseString = objResponse as string;
            }

            return responseString;
        }

        private void BlockTillRateLimitRefresh(
            Tuple<string, string> clientDetails,
            bool isRequestTypeSearch)
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
                var resetSeconds = rateJObject.Value<double>("reset");

                if (remaining == 0)
                {
                    var dt = _epoch.AddSeconds(resetSeconds);
                    var dtNow = DateTime.UtcNow;
                    var timeOut = dt.Subtract(dtNow);

                    _log.Warning($"WARNING: Github api rate limit reached. Sleeping from {dtNow.ToLocalTime()} till {dt.ToLocalTime()}");
                    Thread.Sleep(timeOut);
                }
            }

        }
    }
}
