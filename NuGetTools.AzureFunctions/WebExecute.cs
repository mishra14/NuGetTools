using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Net.Http.Headers;
using System.IO;
using GitLogger.Library;
using System;
using System.Linq;
using NuGetTools.Common;

namespace NuGetTools.AzureFunctions
{
    public static class GitLoggerWebExecute
    {
        private const string _htmlResultFileName = "result.html";
        private const string _csvResultFileName = "result.csv";

        [FunctionName("GitLoggerWebExecute")]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "gitlogger/request")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("GitLogger: C# HTTP trigger function 'GitLoggerWebExecute' processed a request.");
            var response = req.CreateResponse();

            if (req.Method == HttpMethod.Get)
            {                
                var codeRepository = req.GetQueryNameValuePairs().SingleOrDefault(pair => pair.Key == "codeRepoName").Value;
                var branchName = req.GetQueryNameValuePairs().SingleOrDefault(pair => pair.Key == "branchName").Value;
                var issueRepository = req.GetQueryNameValuePairs().SingleOrDefault(pair => pair.Key == "issueRepoName").Value;
                var startCommitSha = req.GetQueryNameValuePairs().SingleOrDefault(pair => pair.Key == "startCommitSha").Value;
                var outputFormat = req.GetQueryNameValuePairs().SingleOrDefault(pair => pair.Key == "outputFormat").Value;

                if (string.IsNullOrEmpty(startCommitSha))
                {
                    response.StatusCode = HttpStatusCode.OK;
                    response.Content = new StringContent(html.gitLoggerRequest);
                    response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
                }
                else if (!string.IsNullOrEmpty(startCommitSha) && 
                    (string.IsNullOrEmpty(codeRepository) || string.IsNullOrEmpty(outputFormat)))
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.Content = new StringContent(html.gitLoggerInputError);
                    response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
                }
                else
                {
                    log.Info($"GitLogger: Collecting commit details for repository '{codeRepository}' from commit '{startCommitSha}'.");
                    var clientDetails = GetAppCredentials(log);
                    issueRepository = ValidateAndPopulateIssueRepo(log, codeRepository, issueRepository);

                    try
                    {
                        var httpUtil = new HttpUtil(new Logger(log));
                        var commits = httpUtil.GetCommits(codeRepository, branchName, startCommitSha, clientDetails);
                        httpUtil.UpdateWithMetadata(codeRepository, issueRepository, commits, clientDetails);
                        GenerateOutputFile(outputFormat, commits, out string resultFilePath, out string responseFileName);
                        GenerateResponseFromOutputFile(response, resultFilePath, responseFileName);
                    }
                    catch (Exception e)
                    {
                        log.Error($"GitLogger: Exception while generating result - {e.Message}");
                        log.Verbose($"GitLogger: {e}");
                        response.StatusCode = HttpStatusCode.InternalServerError;
                        response.Content = new StringContent(html.gitLoggerError);
                        response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
                    }
                }
            }
            else
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                response.Content = new StringContent(html.gitLoggerUnsupported);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            }

            return response;
        }


        private static string ValidateAndPopulateIssueRepo(TraceWriter log, string codeRepository, string issueRepository)
        {
            if (string.IsNullOrEmpty(issueRepository))
            {
                log.Warning($"GitLogger: No issue repository provided. Using code repository for issues.");
                issueRepository = codeRepository;
            }

            return issueRepository;
        }

        private static Tuple<string, string> GetAppCredentials(TraceWriter log)
        {
            var clientDetails = AzureUtil.GetAppCredentials();

            if (string.IsNullOrEmpty(clientDetails.Item1) || string.IsNullOrEmpty(clientDetails.Item2))
            {
                log.Warning($"GitLogger: Unable to read gitlogger app credentials...");
            }
            else
            {
                log.Info($"GitLogger: Gitlogger credentials read as client Id: '{clientDetails.Item1}' and clientSecret: '{clientDetails.Item2}'");
            }

            return clientDetails;
        }

        private static void GenerateResponseFromOutputFile(HttpResponseMessage response, string resultFilePath, string responseFileName)
        {
            var stream = new FileStream(resultFilePath, FileMode.Open);
            response.StatusCode = HttpStatusCode.OK;
            response.Content = new StreamContent(stream);
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = responseFileName
            };

            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            response.Content.Headers.ContentLength = stream.Length;
        }

        private static void GenerateOutputFile(string outputFormat, System.Collections.Generic.IList<Commit> commits, out string resultFilePath, out string responseFileName)
        {
            // Generate temp file to hold the result.
            resultFilePath = Path.GetTempFileName();
            responseFileName = string.Empty;
            switch (outputFormat)
            {
                case "CSV":
                    // Using CSV as interop excel package does not work on an azure function
                    responseFileName = _csvResultFileName;
                    FileUtil.SaveAsCsv(commits, resultFilePath);
                    break;
                default:
                case "HTML":
                    responseFileName = _htmlResultFileName;
                    FileUtil.SaveAsHtml(commits, resultFilePath);
                    break;
            }
        }
    }

    /// <summary>
    /// For getting NuGet Status
    /// </summary>
    public static class NuGetStatusWebExecute
    {
        [FunctionName("NuGetStatusWebExecute")]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "status/request")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("GitLogger: C# HTTP trigger function 'NuGetStatusWebExecute' processed a request.");
            var response = req.CreateResponse();

            if (req.Method == HttpMethod.Get)
            {
                response.StatusCode = HttpStatusCode.OK;
                response.Content = new StringContent(html.nugetStatusRequest);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            }
            else
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                response.Content = new StringContent(html.gitLoggerUnsupported);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            }

            return response;
        }
    }

    /// <summary>
    /// Home Page
    /// </summary>
    public static class HomeWebExecute
    {

        [FunctionName("HomeWebExecute")]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "home")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("GitLogger: C# HTTP trigger function 'HomeWebExecute' processed a request.");
            var response = req.CreateResponse();

            if (req.Method == HttpMethod.Get)
            {
                response.StatusCode = HttpStatusCode.OK;
                response.Content = new StringContent(html.home);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            }
            else
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                response.Content = new StringContent(html.gitLoggerUnsupported);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            }

            return response;
        }
    }
}
