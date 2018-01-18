using GitLogger.Library;
using NuGetTools.Common;
using System;
using System.IO;

namespace GitLogger.App
{
    public class Program
    {
        static void Main(string[] args)
        {
            var startSha = "313a378dbdee355ced26c9501753c1a167286fbf";
            var codeRepository = "NuGet/NuGet.Client";
            var branchName = "dev";
            var issueRepository = "NuGet/Home";
            var useCache = false;

            var logFolder = @"F:\validation\GitLogger\logs";
            var resultCsvPath = Path.Combine(logFolder, "results.csv");
            var resultExcelPath = Path.Combine(logFolder, "results.xlsx");
            var cachePath = Path.Combine(logFolder, "cache");

            FileUtil.ClearLogFiles(resultCsvPath, resultExcelPath, cachePath, useCache);

            Console.WriteLine($"GitLogger: Collecting commit details for repository '{codeRepository}' from commit '{startSha}'");

            var clientDetails = FileUtil.GetAppCredentials(Path.Combine(logFolder, "clientcredentials.txt"));

            var httpUtil = new HttpUtil(new Logger());
            var commits = httpUtil.GetCommits(codeRepository, branchName, startSha, clientDetails);
            httpUtil.UpdateWithMetadata(codeRepository, issueRepository, commits, clientDetails);

            FileUtil.SaveAsCsv(commits, resultCsvPath);
            FileUtil.SaveAsExcel(commits, resultExcelPath);
        }
    }
}
