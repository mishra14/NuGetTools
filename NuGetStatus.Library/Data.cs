using System;
using System.Collections.Generic;
using System.Text;

namespace NuGetStatus.Library
{
    public class Data
    {
        public string Type { get; set; }

        public string SourcePath { get; set; }

        public string LineNumber { get; set; }

        public string ColumnNumber { get; set; }

        public string Code { get; set; }
    }
}
