using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using NuGetStatus.Library;
using NuGetTools.Common;
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
        public static void Run([TimerTrigger("0 0 16 * * 1")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");

            var project = new Project()
            {
                Id = EnvVars.DevDivProjectGuid,
                Name = Constants.DevDiv
            };

            var definitionId = EnvVars.NuGetOfficialBuildDefinitionId;
            var definition = VSTSUtil.GetBuildDefintionAsync(project, Int32.Parse(definitionId)).Result;
            var latestBuild = VSTSUtil.GetLatestBuildAsync(definition).Result;

            if (latestBuild != null)
            {
                latestBuild.PopulateTimeLine().Wait();
            }

            var validations = latestBuild.TimelineRecords.Where(r => r.Name == VALIDATE_VSIX || r.Name == VALIDATE_REPO);
            var logger = new Logger(log);

            var summaries = new List<LocValidationSummary>();

            foreach (var validation in validations)
            {
                var summary = GetLocalizationSummary(validation.Log.GetLogContentAsync(logger).Result);

                if (!string.IsNullOrEmpty(summary))
                {
                    summaries.Add(new LocValidationSummary() { Summary = summary, Title = validation.Name });
                }
            }

            log.Info($"{summaries.Count} summaries found");

            SendEmailViaFlow(GetEmailSummaryInPlainText(summaries), GetEmailSummaryInHtml(summaries), logger).Wait();
        }

        private static string GetLocalizationSummary(string logString)
        {
            var lines = logString.Split('\n');
            var summary = new StringBuilder();
            var logging = false;

            foreach (var line in lines)
            {
                if (logging)
                {
                    summary.AppendLine(line);
                }
                else
                {
                    if (line.Contains(MARKER))
                    {
                        summary.AppendLine(line);
                        logging = true;
                    }
                }
            }

            return summary.ToString();
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
