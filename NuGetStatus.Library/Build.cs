﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NuGetStatus.Library
{
    public class Build
    {
        public Links Links { get; set; }

        public int Id { get; set; }

        public int BuildNumber { get; set; }

        public Status Status { get; set; }

        public Result Result { get; set; }

        public string SourceCommit { get; set; }

        public string SourceBranch { get; set; }

        public BuildDefinition BuildDefinition { get; set; }

        public IList<TimelineRecord> TimelineRecords { get; set; }

        public async Task PopulateTimeLine()
        {
            TimelineRecords = await VSTSUtil.GetBuildTimelineRecordsAsync(this);
        }
    }
}
