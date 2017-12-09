using System;
using System.Collections.Generic;
using System.Text;

namespace NuGetStatus.Library
{
    // Same as status on vsts rest API
    public enum Status
    {
        Unknown = 0,    
        All,
        Failed,
        InProgress,
        None,
        NotStarted,
        PartiallySucceeded,
        Stopped,
        Succeeded,
        Completed
    }
}
