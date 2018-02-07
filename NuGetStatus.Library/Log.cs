﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGetTools.Common;

namespace NuGetStatus.Library
{
    public class Log
    {
        private string _logContent;

        public int Id { get; set; }

        public string Type { get; set; }

        public string Url { get; set; }

        public async Task<string> GetLogContentAsync(Logger log)
        {
            if (_logContent == null)
            {
                _logContent = await VSTSUtil.GetResponseAsync(Url, "application/text");
            }

            return _logContent;
        }
    }
}
