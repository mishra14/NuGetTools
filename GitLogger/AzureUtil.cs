using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitLogger.Library
{
    public static class AzureUtil
    {
        public static Tuple<string, string> GetAppCredentials()
        {
            Tuple<string, string> result = null;
            var appId = Environment.GetEnvironmentVariable("GitLoggerGitHubClientId");
            var appSecret = Environment.GetEnvironmentVariable("GitLoggerGitHubClientSecret");

            result = Tuple.Create(appId, appSecret);

            return result;
        }
    }
}
