using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using NuGetStatus.Library;
using NuGetTools.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace NuGetTools.AzureFunctions
{
    public static class TimedExecute
    {
        private const string MARKER = "================================================================================================================";
        private const string VALIDATE_VSIX = "Validate Vsix Localization";
        private const string VALIDATE_REPO = "Validate Repository Artifacts Localization";

        // Dictionary containing branches to track and the Localization validation API version for those branches
        private static readonly IDictionary<string, int> branchLookUp = new Dictionary<string, int>()
        {
            {"refs/heads/dev", 2 },
            {"refs/heads/release-4.7.0-rtm", 1 }
        };

        // TimerTrigger("0 30 9 * * 1") -> Monday 9:30 am
        // TimerTrigger("0 */1 * * * *") -> Every minute
        // TimerTrigger("0 */30 * * * *") -> Every half hour
        // TimerTrigger("0 0 */1 * * *") -> Every hour
        // TimerTrigger("0 0 6 */1 * *") -> Every day at 6 UTC
        // TimerTrigger("0 0 16 * * 1") -> Every Monday 8:00 am PST
        // [FunctionName("TimedExecute")]
        public static void Run([TimerTrigger("0 0 16 * * 1")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
            log.Info($"C# Timer trigger function running with user: {WindowsIdentity.GetCurrent().Name}");

            var project = new Project()
            {
                Id = EnvVars.DevDivProjectGuid,
                Name = Constants.DevDiv
            };

            var logger = new Logger(log);
            var definitionId = EnvVars.NuGetOfficialBuildDefinitionId;
            var definition = VSTSUtil.GetBuildDefintionAsync(project, Int32.Parse(definitionId), logger).Result;

            foreach (var kvp in branchLookUp)
            {
                var branch = kvp.Key;
                var apiVersion = kvp.Value;
                var latestBuild = VSTSUtil.GetLatestBuildForBranchAsync(definition, logger, branch).Result;

                if (latestBuild != null)
                {
                    ProcessLatestBuildAsync(log, logger, latestBuild, branch, apiVersion).Wait();
                }
                else
                {
                    log.Error($"Latest build for branch {branch} returned null!");
                }
            }
        }

        private static async Task ProcessLatestBuildAsync(TraceWriter log, Logger logger, Build latestBuild, string branch, int apiVersion)
        {
            await latestBuild.PopulateTimeLine(logger);

            var validations = latestBuild.TimelineRecords
                .Where(r => r.Name == VALIDATE_VSIX || r.Name == VALIDATE_REPO);

            switch (apiVersion)
            {
                default:
                case 1:
                    log.Info($"Processing {validations.Count()} validation records for branch {branch} wtih API version {apiVersion} using ProcessValidationRecordsV1");
                    await ProcessValidationRecordsV1(log, logger, validations);
                    break;
                case 2:
                    log.Info($"Processing {validations.Count()} validation records for branch {branch} wtih API version {apiVersion} using ProcessValidationRecordsV2");
                    await ProcessValidationRecordsV2(log, logger, validations);
                    break;
            }
        }

        private static async Task ProcessValidationRecordsV1(TraceWriter log, Logger logger, IEnumerable<TimelineRecord> validations)
        {           
            var summaries = new List<LocValidationSummary>();
            foreach (var validation in validations)
            {
                var summary = GetLocalizationSummaryV1(validation.Log.GetLogContentAsync(logger).Result);

                if (!string.IsNullOrEmpty(summary))
                {
                    summaries.Add(new LocValidationSummary() { Summary = summary, Title = validation.Name });
                }
            }

            log.Info($"{summaries.Count} summaries found");
            await SendEmailViaFlow(GetEmailSummaryInPlainText(summaries), GetEmailSummaryInHtml(summaries), logger);
        }

        private static async Task ProcessValidationRecordsV2(TraceWriter log, Logger logger, IEnumerable<TimelineRecord> validations)
        {
            var summaries = new List<LocalizationResultSummary>();
            foreach (var validation in validations)
            {
                var path = GetResultSummaryPath(validation.Log.GetLogContentAsync(logger).Result);
                if (!string.IsNullOrEmpty(path))
                {
                    using (var file = new StreamReader(path))
                    {
                        summaries.Add(JsonConvert.DeserializeObject<LocalizationResultSummary>(file.ReadToEnd()));
                    }
                }
            }

            log.Info($"{summaries.Count} summaries found");
            await SendEmailViaFlow(GetEmailSummaryInPlainText(summaries), GetEmailSummaryInHtml(summaries), logger);
        }

        private static string GetResultSummaryPath(string logString)
        {
            var pathLine = logString.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
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

        private static string GetLocalizationSummaryV1(string logString)
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

        private static string GetEmailSummaryInHtml(List<LocalizationResultSummary> summaries)
        {
            var builder = new StringBuilder();

            summaries.ForEach(s => builder.AppendLine(s.ToEmailString()));

            return builder.ToString();
        }

        private static string GetEmailSummaryInPlainText(List<LocalizationResultSummary> summaries)
        {
            var builder = new StringBuilder();

            summaries.ForEach(s => builder.AppendLine(s.ToEmailString()));

            return builder.ToString();
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

                if (response.StatusCode != HttpStatusCode.OK || 
                    response.StatusCode != HttpStatusCode.Accepted)
                {
                    logger.Info($"Response: {await response.Content.ReadAsStringAsync()}");
                }
            }
        }
    }
}
