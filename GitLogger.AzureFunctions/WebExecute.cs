using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Net.Http.Headers;

namespace GitLogger.AzureFunctions
{
    public static class WebExecute
    {
        [FunctionName("WebExecute")]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "request")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            if (req.Method == HttpMethod.Get)
            {
                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent("<!DOCTYPE html><html><body><h1>My First Heading</h1><p>My first paragraph.</p></body></html>");
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
                return response;
            }
            else if (req.Method == HttpMethod.Put)
            {
                // Fetching the name from the path parameter in the request URL
                return req.CreateResponse(HttpStatusCode.OK, "PUT: Hello ");
            }


            // Fetching the name from the path parameter in the request URL
            return req.CreateResponse(HttpStatusCode.BadRequest, "Error, this app only supports Get and Put");

        }

    }
}
