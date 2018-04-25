using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using NuGetStatus.Library;
using NuGetTools.Common;
using NuGetValidator.Localization;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace NuGetTools.AzureFunctions
{
    public static class TimedExecute
    {
        private const string MARKER = "================================================================================================================";
        private const string VALIDATE_VSIX = "Validate Vsix Localization";
        private const string VALIDATE_REPO = "Validate Repository Artifacts Localization";

        // TimerTrigger("0 30 9 * * 1") -> Monday 9:30 am
        // TimerTrigger("0 */1 * * * *") -> Every minute
        // TimerTrigger("0 0 */1 * * *") -> Every hour
        // TimerTrigger("0 0 6 */1 * *") -> Every day at 6 UTC
        // TimerTrigger("0 0 16 * * 1") -> Every Monday 8:00 am PST
        [FunctionName("TimedExecute")]
        public static void Run([TimerTrigger("0 */1 * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");

            var project = new Project()
            {
                Id = EnvVars.DevDivProjectGuid,
                Name = Constants.DevDiv
            };

            var logger = new Logger(log);

            var definitionId = EnvVars.NuGetOfficialBuildDefinitionId;
            var definition = VSTSUtil.GetBuildDefintionAsync(project, Int32.Parse("8118"), logger).Result;
            var latestBuild = VSTSUtil.GetBuildAsync(definition, "1628559", logger).Result;
            //var latestBuild = VSTSUtil.GetLatestBuildAsync(definition, logger).Result;

            if (latestBuild != null)
            {
                latestBuild.PopulateTimeLine(logger).Wait();
            }

            var validations = latestBuild.TimelineRecords.Where(r => r.Name == VALIDATE_VSIX || r.Name == VALIDATE_REPO);

            foreach (var validation in validations)
            {
                var path = GetResultSummaryPath(validation.Log.GetLogContentAsync(logger).Result);

                if (!string.IsNullOrEmpty(path))
                {
                    using (var file = new StreamReader(path))
                    {
                        var resultSummary = JsonConvert.DeserializeObject<ResultSummary>(file.ReadToEnd());
                    }
                }
            }

            //log.Info($"{summaries.Count} summaries found");

            //SendEmailViaFlow(GetEmailSummaryInPlainText(summaries), GetEmailSummaryInHtml(summaries), logger).Wait();
        }

        private static string GetResultSummaryPath(string logString)
        {
            var pathLine = logString.Split(new char[]{'\n', '\r'}, StringSplitOptions.RemoveEmptyEntries)
                .Where(l => l.Contains("Path:") && l.Contains("ResultSummary.json"))
                .FirstOrDefault();
            string path = null;

            if (!string.IsNullOrEmpty(pathLine))
            {
                path = pathLine.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();

                if (path != null)
                {
                    path = path.Trim();
                }
            }

            return path;
        }

        private static string GetEmailSummaryInHtml(List<LocValidationSummary> summaries)
        {
            var builder = new StringBuilder();

            summaries.ForEach(s => builder.AppendLine(s.ToEmailString()));

            return builder.ToString();
        }

        private static string GetEmailSummaryInPlainText(List<LocValidationSummary> summaries)
        {
            var builder = new StringBuilder();

            summaries.ForEach(s => builder.AppendLine(s.ToEmailString()));

            return builder.ToString();
        }

        private static async Task SendEmailViaSendgrid(string plainTextMessage, string htmlMessage, Logger logger) 
        {
            var SendgridPat = EnvVars.SendgridPat;
            var sourceMailAddress = EnvVars.SrcEmailAddress;
            var destMailAddress = EnvVars.DestEmailAddress;

            logger.Info($"Sending email to {sourceMailAddress} with the following message - {Environment.NewLine}{plainTextMessage}");

            var client = new SendGridClient(SendgridPat);
            var from = new EmailAddress(sourceMailAddress);
            var subject = $"Localization status report {DateTime.Now}";
            var to = new EmailAddress(destMailAddress);
            var plainTextContent = plainTextMessage;
            var htmlContent = htmlMessage;
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg);
        }

        private static async Task SendEmailViaFlow(string plainTextMessage, string htmlMessage, Logger logger)
        {
            var destEmailAddress = EnvVars.DestEmailAddress;
            var flowUrl = EnvVars.FlowUrl;

            logger.Info($"Sending email to {destEmailAddress} with the following message - {Environment.NewLine}{plainTextMessage}");

            using (var httpClient = new HttpClient())
            {
                var values = new Dictionary<string, string>()
                {
                    { Constants.Subject, $"Localization status report {DateTime.Now}"},
                    { Constants.Content, htmlMessage},
                    { Constants.Destination, destEmailAddress}
                };

                var response = await httpClient.PostAsJsonAsync(flowUrl, values);

                logger.Info($"Response Code: {response.StatusCode}");

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    logger.Info($"Response: {await response.Content.ReadAsStringAsync()}");
                }
            }
        }
    }
}
