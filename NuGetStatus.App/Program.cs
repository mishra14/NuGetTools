using NuGetStatus.Library;
using System;
using System.Linq;

namespace NuGetStatus.App
{
    class Program
    {
        private const string VALIDATE_VSIX = "Validate Vsix Localization";
        private const string VALIDATE_REPO = "Validate Repository Artifacts Localization";

        static void Main(string[] args)
        {
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
            var log = new NuGetTools.Common.Logger();
            foreach (var validation in validations)
            {
                var summary = GetLocalizationSummary(validation.Log.GetLogContent(log));
            }

        }

        static string GetLocalizationSummary(string log)
        {

            return "";
        }
    }
}
