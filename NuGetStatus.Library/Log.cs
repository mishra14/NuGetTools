using System;
using System.Collections.Generic;
using System.Text;
using NuGetTools.Common;

namespace NuGetStatus.Library
{
    public class Log
    {
        public int Id { get; set; }

        public string Type { get; set; }

        public string Url { get; set; }

        public string GetLogContent(Logger log)
        {        
            return HttpUtil.GetHttpResponse(Url, log);
        }
    }
}
