using NuGetStatus.Library;
using System;

namespace NuGetStatus.App
{
    class Program
    {
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

        }
    }
}
