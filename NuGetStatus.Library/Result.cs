using System;
using System.Collections.Generic;
using System.Text;

namespace NuGetStatus.Library
{
    // Same as result on vsts rest API
    public enum Result
    {
        Unknown = 0,
        Succeeded,
        PartiallySucceeded,
        Failed,
        Cancelled
    }
}
