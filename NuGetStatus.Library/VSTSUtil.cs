using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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


        public static async Task<BuildDefinition> GetBuildDefintionAsync(Project project, int buildDefinitionId)
        {
            var url = $@"{Url.DevDivUrl}/{Constants.DefaultCollection}/{project.Id}/{Constants.Apis}/{Constants.Build}/{Constants.Definitions}/{buildDefinitionId}";

            var json = await GetJsonResponseAsync(url);

            return new BuildDefinition()
            {
                Id = buildDefinitionId,
                Name = GetString(json, Constants.Name),
                Project = project,
                Links = GetLinks(json)
            };
        }

        public static async Task<Build> GetLatestBuildAsync(BuildDefinition definition)
        {
            var url = $@"{Url.DevDivUrl}/{Constants.DefaultCollection}/{definition.Project.Id}/{Constants.Apis}/{Constants.Build}/{Constants.Builds}?{Constants.Definitions}={definition.Id}&{Constants.StatusFilter}={Status.Completed.ToString()}&${Constants.Top}=1";

            var response = await GetJsonResponseAsync(url);
            var json = response[Constants.Value]?.Value<JArray>()[0];

            return new Build()
            {
                Id = GetInt(json, Constants.Id),
                BuildNumber = GetInt(json, Constants.BuildNumber),
                Status = GetEnum<Status>(json, Constants.Status),
                Result = GetEnum<Result>(json, Constants.Result),
                Links = GetLinks(json),
                SourceBranch = GetString(json, Constants.SourceBranch),
                SourceCommit = GetString(json, Constants.SourceVersion),
                BuildDefinition = definition
            };
        }

        public static async Task<IList<TimelineRecord>> GetBuildTimelineRecordsAsync(Build build)
        {
            var url = $@"{Url.DevDivUrl}/{Constants.DefaultCollection}/{build.BuildDefinition.Project.Id}/{Constants.Apis}/{Constants.Build}/{Constants.Builds}/{build.Id}/{Constants.Timeline}";

            var response = await GetJsonResponseAsync(url);
            var recordsArray = response[Constants.Records]?.Value<JArray>();
            var records = new List<TimelineRecord>();

            if (recordsArray != null && recordsArray.HasValues)
            {
                foreach (var json in recordsArray)
                {
                    var timelineRecord = new TimelineRecord
                    {
                        Id = GetString(json, Constants.Id),
                        ParentId = GetString(json, Constants.ParentId),
                        Type = GetString(json, Constants.Type),
                        Name = GetString(json, Constants.Name),
                        Status = GetEnum<Status>(json, Constants.State),
                        Result = GetEnum<Result>(json, Constants.Result),
                        WarningCount = GetInt(json, Constants.WarningCount),
                        ErrorCount = GetInt(json, Constants.ErrorCount),
                        Log = GetLog(json),
                        Issues = GetIssues(json)
                    };

                    records.Add(timelineRecord);
                }
            }

            return records;
        }

        private static IList<Issue> GetIssues(JToken parentJson)
        {
            var issuesArray = parentJson[Constants.Issues]?.Value<JArray>();
            var issues = new List<Issue>();

            if (issuesArray != null && issuesArray.HasValues)
            {
                foreach (var json in issuesArray)
                {
                    var issue = new Issue()
                    {
                        Type = GetString(json, Constants.Type),
                        Category = GetString(json, Constants.Category),
                        Message = GetString(json, Constants.Message),
                        Data = GetData(json),
                    };

                    issues.Add(issue);
                }
            }

            return issues;
        }

        private static Data GetData(JToken parentJson)
        {
            var json = parentJson[Constants.Log];
            Data data = null;

            if (json != null && json.HasValues)
            {
                data = new Data()
                {
                    Type = GetString(json, Constants.Type),
                    SourcePath = GetString(json, Constants.SourcePath),
                    LineNumber = GetString(json, Constants.LineNumber),
                    ColumnNumber = GetString(json, Constants.ColumnNumber),
                    Code = GetString(json, Constants.Code),
                };
            }

            return data;
        }

        private static Log GetLog(JToken parentJson)
        {
            var json = parentJson[Constants.Log];
            Log log = null;

            if (json != null && json.HasValues)
            {
                try
                {
                    log = new Log()
                    {
                        Id = GetInt(json, Constants.Id),
                        Type = GetString(json, Constants.Type),
                        Url = GetString(json, Constants.Url)
                    };
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            return log;
        }

        public static async Task<Release> GetReleaseAsync(Build build)
        {
            var url = $@"{Url.DevDivReleaseUrl}/{Constants.Apis}/{Constants.Release}/{Constants.Releases}?{Constants.ArtifactTypeId}={Constants.Build}&{Constants.ArtifactVersionId}={build.Id}&{Constants.SourceId}={build.BuildDefinition.Project.Id}:{build.BuildDefinition.Id}";

            var response = await GetJsonResponseAsync(url);
            var json = response[Constants.Value]?.Value<JArray>()[0];

            return new Release()
            {
                Id = GetInt(json, Constants.Id),
                Status = GetEnum<Status>(json, Constants.Status),
                Links = GetLinks(json),
                Name = GetString(json, Constants.Name),
                Build = build
            };
        }

        private static TEnum GetEnum<TEnum>(JToken json, string key) where TEnum : struct
        {
            var statusString = json[key]?.Value<string>();

            return Enum.TryParse(statusString, ignoreCase: true, result: out TEnum result) ? result : default(TEnum);
        }

        private static string GetString(JToken json, string key)
        {
            return json[key]?.Value<string>() ?? string.Empty;
        }

        private static int GetInt(JToken json, string key)
        {
            return json[key]?.Value<int>() ?? -1;
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

        public static async Task<string> GetResponseAsync(string requestUrl, string responseType = "application/json")
        {
            var result = string.Empty;

            try
            {
                var personalaccesstoken = EnvVars.VstsPat;

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue(responseType));

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
