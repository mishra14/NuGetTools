using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Net.Http.Headers;
using System.IO;

namespace GitLogger.AzureFunctions
{
    public static class WebExecute
    {
        public static string InputPage = "<!DOCTYPE html>" + 
            "<html>" + 
            "<body>" + "" +
            "<h1><a href=\"https://github.com/mishra14/GitLogger\">GitLogger</a></h1>" + 
            "<form>" + 
            "Commit SHA: <input type=\"text\" name=\"commitSha\"><br><br>" + 
            "Repository Name:  <input type=\"text\" name=\"repoName\" value=\"nuget/nuget.client\"><br><br>" + 
            "<input type=\"button\" name=\"submit\" value=\"Submit\"><br><br>" + 
            "</form>" + 
            "</body>" + 
            "</html>";

        [FunctionName("WebExecute")]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "request")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");
            var response = req.CreateResponse();


            if (req.Method == HttpMethod.Get)
            {
                response.StatusCode = HttpStatusCode.OK;
                response.Content = new StringContent(InputPage);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
                return response;
            }
            else if (req.Method == HttpMethod.Put)
            {
                var repository = string.Empty;
                var commitSha = string.Empty;

                log.Verbose($"GitLogger: Collecting commit details for repository '{repository}' from commit '{commitSha}'");

                var clientDetails = FileUtil.GetAppCredentials(Path.Combine("", "clientcredentials.txt"));
                var commits = HttpUtil.GetCommits(repository, commitSha, clientDetails);
                HttpUtil.UpdateWithMetadata(repository, commits, clientDetails);

                // Fetching the name from the path parameter in the request URL
                return req.CreateResponse(HttpStatusCode.OK, "PUT: Hello ");
            }
            else
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                response.Content = new StringContent("<!DOCTYPE html><html><body><h1>GitLogger</h1><p>Bad Request. This app only supports GET and PUT requests.</p></body></html>");
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            }

            return response;

        }

    }
}
