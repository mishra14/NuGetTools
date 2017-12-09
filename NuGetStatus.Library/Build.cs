using System;
using System.Collections.Generic;
using System.Text;

namespace NuGetStatus.Library
{
    public class Build
    {
        public Links Links { get; set; }

        public int Id { get; set; }

        public int BuildNumber { get; set; }

        public Status Status { get; set; }

        public string Result { get; set; }

        public string SourceCommit { get; set; }

        public string SourceBranch { get; set; }
    }
}
