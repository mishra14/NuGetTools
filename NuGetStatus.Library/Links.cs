using System;
using System.Collections.Generic;
using System.Text;

namespace NuGetStatus.Library
{
    public class Links
    {
        /// <summary>
        /// Link to the json page.
        /// </summary>
        public string Self { get; set; }

        /// <summary>
        /// Link to web page.
        /// </summary>
        public string Web { get; set; }

        /// <summary>
        /// Commit. Can be Null.
        /// </summary>
        public string Commit { get; set; }

        /// <summary>
        /// Timeline. Can be Null.
        /// </summary>
        public string Timeline { get; set; }

        /// <summary>
        /// Badge. Can be Null.
        /// </summary>
        public string Badge { get; set; }
    }
}
