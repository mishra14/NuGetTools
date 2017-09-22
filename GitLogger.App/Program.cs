﻿using GitLogger.Library;
using System;
using System.IO;

namespace GitLogger.App
{
    public class Program
    {
        static void Main(string[] args)
        {
            var startSha = "313a378dbdee355ced26c9501753c1a167286fbf";
            var repository = "nuget/nuget.client";
            var useCache = false;

            var logFolder = @"F:\validation\GitLogger\logs";
            var resultCsvPath = Path.Combine(logFolder, "results.csv");
            var resultExcelPath = Path.Combine(logFolder, "results.xlsx");
            var cachePath = Path.Combine(logFolder, "cache");

            FileUtil.ClearLogFiles(resultCsvPath, resultExcelPath, cachePath, useCache);

            Console.WriteLine($"GitLogger: Collecting commit details for repository '{repository}' from commit '{startSha}'");

            var clientDetails = FileUtil.GetAppCredentials(Path.Combine(logFolder, "clientcredentials.txt"));

            var commits = HttpUtil.GetCommits(repository, startSha, clientDetails);
            HttpUtil.UpdateWithMetadata(repository, commits, clientDetails);

            FileUtil.SaveAsCsv(commits, resultCsvPath);
            FileUtil.SaveAsExcel(commits, resultExcelPath);
        }
    }
}