using System;
using System.Collections.Generic;
using System.IO;

namespace GitLogger
{
    class Program
    {
        static void Main(string[] args)
        {

            var startSha = "313a378dbdee355ced26c9501753c1a167286fbf";
            var repository = "nuget/nuget.client";
            var logPath = @"F:\validation\GitLogger\logs\result.csv";

            Console.WriteLine($"GitLogger: Collecting commit details for repository '{repository}' from commit '{startSha}'");
            var commits = HttpUtil.GetCommits(repository, startSha);
            foreach (var commit in commits)
            {
                //commit.PopulateIssueAndPRData();
            }

            FileUtil.SaveAsCsv(commits, logPath);
        }
    }
}
