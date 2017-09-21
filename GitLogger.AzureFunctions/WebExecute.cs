using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Net.Http.Headers;
using System.IO;
using GitLogger.Library;

namespace GitLogger.AzureFunctions
{
    public static class WebExecute
    {

        [FunctionName("WebExecute")]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "request")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("GitLogger: C# HTTP trigger function processed a request.");
            var response = req.CreateResponse();


            if (req.Method == HttpMethod.Get)
            {                
                var repository = req.GetQueryNameValuePairs().SingleOrDefault(pair => pair.Key == "repoName").Value;
                var commitSha = req.GetQueryNameValuePairs().SingleOrDefault(pair => pair.Key == "commitSha").Value;

                if (string.IsNullOrEmpty(repository) || string.IsNullOrEmpty(commitSha))
                {
                    response.StatusCode = HttpStatusCode.OK;
                    response.Content = new StringContent(html.request);
                    response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
                }
                else
                {
                    log.Info($"GitLogger: Collecting commit details for repository '{repository}' from commit '{commitSha}'.");

                    var clientDetails = AzureUtil.GetAppCredentials();

                    if (string.IsNullOrEmpty(repository) || string.IsNullOrEmpty(commitSha))
                    {
                        log.Info($"GitLogger: Unable to read gitlogger app credentials.");
                        response.StatusCode = HttpStatusCode.ExpectationFailed;
                        response.Content = new StringContent(html.error);
                        response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
                    }
                    else
                    {
                        log.Info($"GitLogger: Gitlogger credentials read as client Id: '{clientDetails.Item1}' and clientSecret: '{clientDetails.Item2}'");

                        var commits = HttpUtil.GetCommits(repository, commitSha, clientDetails);
                        HttpUtil.UpdateWithMetadata(repository, commits, clientDetails);

                        // Generate temp file to hold the result.
                        var resultFilePath = Path.GetTempFileName();
                        FileUtil.SaveAsExcel(commits, resultFilePath);


                        var stream = new FileStream(resultFilePath, FileMode.Open);

                        response.StatusCode = HttpStatusCode.OK;
                        response.Content = new StreamContent(stream);
                        response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                        {
                            FileName = "result.xlsx"
                        };

                        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                        response.Content.Headers.ContentLength = stream.Length;
                    }
                }
            }
            else
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                response.Content = new StringContent(html.unsupported);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            }

            return response;
        }
    }
}
