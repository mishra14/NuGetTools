using System;

namespace NuGetStatus.Library
{
    public class EnvVars
    {
        public static readonly string VstsPat = Environment.GetEnvironmentVariable(nameof(VstsPat)) ?? throw new InvalidOperationException("Unable to read vsts pat!");

        public static readonly string SendgridPat = Environment.GetEnvironmentVariable(nameof(SendgridPat)) ?? throw new InvalidOperationException("Unable to read snedgrid pat!");

        public static readonly string DestEmailAddress = Environment.GetEnvironmentVariable(nameof(DestEmailAddress)) ?? throw new InvalidOperationException("Unable to read destination email!");

        public static readonly string SrcEmailAddress = Environment.GetEnvironmentVariable(nameof(SrcEmailAddress)) ?? throw new InvalidOperationException("Unable to read source email!");

        public static readonly string DevDivProjectGuid = Environment.GetEnvironmentVariable(nameof(DevDivProjectGuid)) ?? throw new InvalidOperationException("Unable to read devdiv project guid!");

        public static readonly string NuGetOfficialBuildDefinitionId = Environment.GetEnvironmentVariable(nameof(NuGetOfficialBuildDefinitionId)) ?? throw new InvalidOperationException("Unable to read nuget build definition id!");

        public static readonly string FlowUrl = Environment.GetEnvironmentVariable(nameof(FlowUrl)) ?? throw new InvalidOperationException("Unable to read flow url!");
    }
}
