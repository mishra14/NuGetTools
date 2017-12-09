using System;
using System.Collections.Generic;
using System.Text;

namespace NuGetStatus.Library
{
    public class Project
    {
        public string Name { get; set; }

        public string Id { get; set; }

        public Links Links { get; set; }
    }
}
