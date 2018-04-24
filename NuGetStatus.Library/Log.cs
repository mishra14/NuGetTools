using System.Threading.Tasks;
using NuGetTools.Common;

namespace NuGetStatus.Library
{
    public class Log
    {
        private string _logContent;

        public int Id { get; set; }

        public string Type { get; set; }

        public string Url { get; set; }

        public async Task<string> GetLogContentAsync(Logger logger)
        {
            if (_logContent == null)
            {
                _logContent = await VSTSUtil.GetResponseAsync(Url, logger, "application/text");
            }

            return _logContent;
        }
    }
}
