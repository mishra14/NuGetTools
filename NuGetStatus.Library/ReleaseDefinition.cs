using System;
using System.Collections.Generic;
using System.Text;

namespace NuGetStatus.Library
{
    public class ReleaseDefinition
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public Links Links { get; set; }
    }
}
