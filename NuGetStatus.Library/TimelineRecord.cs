using System;
using System.Collections.Generic;
using System.Text;

namespace NuGetStatus.Library
{
    public class TimelineRecord
    {
        public string Id { get; set; }

        public string ParentId { get; set; }

        public string Type { get; set; }

        public string Name { get; set; }

        public Status Status { get; set; }

        public Result Result { get; set; }

        public Log Log { get; set; }

        public int WarningCount { get; set; }

        public int ErrorCount { get; set; }

        public IList<Issue> Issues { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
