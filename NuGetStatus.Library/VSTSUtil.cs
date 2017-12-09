using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace NuGetStatus.Library
{
    public class VSTSUtil
    {
        // build definition api example - https://devdiv.visualstudio.com/DefaultCollection/0bdbc590-a062-4c3f-b0f6-9383f67865ee/_apis/build/definitions/5868

        // latest build
        // build api - https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_apis/build/builds?definitions={buildDefinitionId}&statusFilter={status}&$top=1&[api-version=2.0]
        // build api example - https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_apis/build/builds?definitions=5868&statusFilter=completed&$top=1

        // release for that build
        // release api - https://devdiv.vsrm.visualstudio.com/_apis/Release/releases?artifactTypeId=Build&sourceId={projectIdGuid}:{buildDefinitionId}&artifactVersionId={buildId}
        // release api example - https://devdiv.vsrm.visualstudio.com/_apis/Release/releases?artifactTypeId=Build&sourceId=0bdbc590-a062-4c3f-b0f6-9383f67865ee:5868&artifactVersionId=1208980


        // environments for that release
        // release definition api - https://devdiv.vsrm.visualstudio.com/{projectIdGuid}/_apis/Release/releases/{releaseId}
        // release definition api example - https://devdiv.vsrm.visualstudio.com/0bdbc590-a062-4c3f-b0f6-9383f67865ee/_apis/Release/releases/64073
        
        // read VSTS Personal Access Token from environment variables
        private static string VstsPat = Environment.GetEnvironmentVariable(Constants.VstsPatEnvVarName);


        public static async Task<BuildDefinition> GetBuildDefintionAsync(Project project, int buildDefinitionId)
        {
            var url = $@"{Url.DevDivUrl}/{Constants.DefaultCollection}/{project.Id}/{Constants.Apis}/{Constants.Build}/{Constants.Definitions}/{buildDefinitionId}";

            var response = await GetJsonResponseAsync(url);

            return new BuildDefinition()
            {
                Id = buildDefinitionId,
                Name = GetName(response),
                Project = project,
                Links = GetLinks(response)
            };
        }

        public static async Task<Build> GetLatestBuildAsync(BuildDefinition definition)
        {
            var url = $@"{Url.DevDivUrl}/{Constants.DefaultCollection}/{definition.Project.Id}/{Constants.Apis}/{Constants.Build}/{Constants.Builds}?{Constants.Definitions}={definition.Id}&{Constants.StatusFilter}={Status.Completed.ToString()}&${Constants.Top}=1";

            var response = await GetJsonResponseAsync(url);

            var buildJson = response[Constants.Value]?.Value<JArray>()[0];

            return new Build()
            {
                Id = GetId(buildJson),
                BuildNumber = GetBuildNumber(buildJson),
                Status = GetStatus(buildJson),
                Links = GetLinks(buildJson),

            };
        }

        private static Status GetStatus(JToken json)
        {
            var statusString =  json[Constants.Status].Value<string>();

            return Enum.TryParse(statusString, ignoreCase: true, result: out Status status) ? status : Status.Unknown;
        }

        private static int GetBuildNumber(JToken json)
        {
            return json[Constants.BuildNumber].Value<int>();
        }

        private static int GetId(JToken json)
        {
            return json[Constants.Id].Value<int>();
        }

        private static string GetName(JToken json)
        {
            return json[Constants.Name].Value<string>();
        }

        private static Links GetLinks(JToken json)
        {
            return new Links()
            {
                Self = json[Constants.Links][Constants.Self][Constants.HRef].Value<string>(),
                Badge = json[Constants.Links]?[Constants.Badge]?[Constants.HRef]?.Value<string>(),
                Web = json[Constants.Links][Constants.Web][Constants.HRef].Value<string>()
            };
        }

        private static async Task<JObject> GetJsonResponseAsync(string requestUrl)
        {
            var response = await GetResponseAsync(requestUrl);

            var jObject = JObject.Parse(response);

            return jObject;
        }

        private static async Task<string> GetResponseAsync(string requestUrl)
        {
            var result = string.Empty;

            try
            {
                var personalaccesstoken = VstsPat ?? throw new ArgumentNullException(nameof(VstsPat));

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));

                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", "", personalaccesstoken))));

                    using (var response = client.GetAsync(requestUrl).Result)
                    {
                        response.EnsureSuccessStatusCode();
                        result = await response.Content.ReadAsStringAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return result;
        }
    }
}
