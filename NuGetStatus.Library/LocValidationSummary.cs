using System;

namespace NuGetStatus.Library
{
    public class LocValidationSummary
    {
        public string Title { get; set; }

        public string Summary { get; set; }

        public override string ToString()
        {
            return $"{Title}{Environment.NewLine}{Summary}";
        }

        public string ToEmailString()
        {
            return $"<h1>{Title}</h1>{Environment.NewLine}<p>{Summary.Replace("\r\n", "</br>").Replace("\r", "</br>").Replace("\n", "</br>")}</p>";
        }
    }
}