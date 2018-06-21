using NuGetValidator.Localization;
using System;

namespace NuGetStatus.Library
{
    public class LocalizationResultSummary : ResultSummary
    {
        public override string ToString()
        {
            return $"{ExecutionType}{Environment.NewLine}{ToJson()}";
        }

        public string ToEmailString()
        {
            return $"<h1>{ExecutionType}</h1>{Environment.NewLine}<p>{ToString().Replace("\r\n", "</br>").Replace("\r", "</br>").Replace("\n", "</br>")}</p>";
        }
    }
}
