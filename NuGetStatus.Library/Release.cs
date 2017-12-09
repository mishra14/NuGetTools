using System;
using System.Collections.Generic;
using System.Text;

namespace NuGetStatus.Library
{
    public class Release
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public Status Status { get; set; }

        public ReleaseDefinition ReleaseDefinition { get; set; }

        public Build Build { get; set; }

        public Links Links { get; set; }
    }
}
