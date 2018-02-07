using System;
using System.Collections.Generic;
using System.Linq;
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

        [FunctionName("TimedExecute")]
        public static void Run([TimerTrigger("0 30 9 * * 1")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");

            var project = new Project()
            {
                Id = Constants.DevDivProjectGuid,
                Name = Constants.DevDiv
            };

            var definitionId = Constants.NuGetOfficialYamlBuildDefinitionId;
            var definition = VSTSUtil.GetBuildDefintionAsync(project, definitionId).Result;
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

            SendEmail(GetEmailSummaryInPlainText(summaries), GetEmailSummaryInHtml(summaries), logger).Wait();
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

        private static async Task SendEmail(string plainTextMessage, string htmlMessage, Logger logger) 
        {
            var sendGridPat = Environment.GetEnvironmentVariable(Constants.SendgridPatEnvVarName) ?? throw new InvalidOperationException("Unable to read sendgrid PAT!");
            var sourceMailAddress = Environment.GetEnvironmentVariable(Constants.SendgridSrcEmailAddress) ?? throw new InvalidOperationException("Unable to read source email for sendgrid!");
            var destMailAddress = Environment.GetEnvironmentVariable(Constants.SendgridDestEmailAddress) ?? throw new InvalidOperationException("Unable to read destination email for sendgrid!");

            logger.Info($"Sending email to {sourceMailAddress} with the following message -{Environment.NewLine}{plainTextMessage}");

            var client = new SendGridClient(sendGridPat);
            var from = new EmailAddress(sourceMailAddress);
            var subject = $"Localization status report {DateTime.Now}";
            var to = new EmailAddress(destMailAddress);
            var plainTextContent = plainTextMessage;
            var htmlContent = htmlMessage;
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg);
        }
    }
}
