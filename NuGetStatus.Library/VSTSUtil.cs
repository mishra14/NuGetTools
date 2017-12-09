using System;

namespace NuGetStatus.Library
{
    public class VSTSUtil
    {
        // build definition api - https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_apis/build/builds?definitions={buildDefinitionId}&statusFilter={status}&$top=1&api-version=2.0
        // build definition api example - https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_apis/build/builds?definitions=5868&statusFilter=completed&$top=1&api-version=2.0
        // release definition api - https://devdiv.vsrm.visualstudio.com/_apis/Release/releases?artifactTypeId=Build&sourceId={projectIdGuid}:{buildDefinitionId}&artifactVersionId={buildId}
        // release definition api example - https://devdiv.vsrm.visualstudio.com/_apis/Release/releases?artifactTypeId=Build&sourceId=0bdbc590-a062-4c3f-b0f6-9383f67865ee:5868&artifactVersionId=1208980

    }
}
