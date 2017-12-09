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

            var definitionId = Constants.NuGetOfficialBuildDefinitionId;

            var definition = VSTSUtil.GetBuildDefintionAsync(project, definitionId).Result;

            var latestBuild = VSTSUtil.GetLatestBuildAsync(definition).Result;

        }
    }
}
